using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using appbox.Serialization;
using appbox.Server;
using Newtonsoft.Json;

namespace appbox.Store
{
    public static class BlobStore
    {
        private const int MaxPathSize = 2048; //TODO:暂限定路径长度
        private const int SegmentSize = 64 * 1024;
        private static int CmdIdSeq; //用于生成当前节点的命令标识

        internal const byte KEY_TYPE_PATH = 0;
        internal const byte KEY_TYPE_FILE = 1;

        #region ====Write Methods====
        public static async ValueTask UploadAsync(byte appId, Stream stream, string toPath)
        {
            //TODO:判断长度超过限制
            if (!toPath.StartsWith('/'))
                throw new ArgumentException("Path must start with '/'", nameof(toPath));
            if (string.IsNullOrEmpty(Path.GetFileName(toPath)))
                throw new ArgumentException("Path must has file name", nameof(toPath));

            var needSize = (uint)stream.Length;

            //1. 先生成写事务标识
            Guid txnId = GenWriteTxnId();

            //2. 提议至BlobMetaRaftGroup准备写
            var res = await PrepareWriteAsync(appId, txnId, toPath, needSize);
            if (res == null)
                throw new Exception("准备写结果为空");

            PrepareWriteResult result = ParsePrepareResult(res);
            res.Dispose();
            if (result.ChunkRaftGroupId == 0) //上级目录没有可用的Chunk，则提议创建新的
            {
                res = await TryCreateChunkAsync(appId, Path.GetDirectoryName(toPath), needSize);
                result = ParsePrepareResult(res);
                res.Dispose();
            }

            //Log.Debug($"ChunkRaftGroupId = {result.ChunkRaftGroupId}  8864812498945");

            //3. 循环写入块，最后一块带结束标记(引擎计算新旧文件大小差异)
            //TODO:优化内存复制
            string fileName = Path.GetFileName(toPath);
            byte[] fileNameData = System.Text.Encoding.UTF8.GetBytes(fileName);
            int pathSize = 32 + fileNameData.Length;
            IntPtr pathPtr = Marshal.AllocHGlobal(pathSize);
            unsafe
            {
                var idPtr = (Guid*)pathPtr.ToPointer();
                idPtr[0] = result.ParentPathId;
                idPtr[1] = txnId;

                var namePtr = pathPtr + 32;
                Marshal.Copy(fileNameData, 0, namePtr, fileNameData.Length);
            }

            var buffer = new byte[SegmentSize];
            int bytesRead = 0;
            int totalRead = 0;
            uint option = 0;
            IntPtr nativeDataPtr;
            do
            {
                bytesRead = stream.Read(buffer);
                if (bytesRead <= 0)
                    break;
                totalRead += bytesRead;
                if (totalRead >= stream.Length)
                    option = 2;
                unsafe
                {
                    nativeDataPtr = NativeApi.NewNativeString(bytesRead, out byte* dataPtr);
                    Marshal.Copy(buffer, 0, new IntPtr(dataPtr), bytesRead);
                }
                await StoreApi.Api.BlobWriteChunkAsync(result.ChunkRaftGroupId, pathPtr, (uint)pathSize, option, nativeDataPtr);
                if (option == 2)
                    break;
                option = 1;
            } while (true);

            //释放分配的pathPtr
            buffer = null;
            Marshal.FreeHGlobal(pathPtr);
        }

        /// <summary>
        /// 提议至BlobMetaRaftGroup准备写
        /// </summary>
        /// <remarks>
        /// 1.不存在BlobMetaRaftGroup则创建;
        /// 2.不存在上级路径则创建;
        /// </remarks>
        private static async ValueTask<INativeData> PrepareWriteAsync(byte appId, Guid txnId, string path, uint needSize)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            var pathData = System.Text.Encoding.UTF8.GetBytes(path);
            if (pathData.Length > MaxPathSize)
                throw new ArgumentException("Path length out of range", nameof(path));

            IntPtr cmdIdPtr;
            IntPtr pathPtr;
            unsafe
            {
                cmdIdPtr = new IntPtr(&txnId);

                //暂复制路径
                var pathDataPtr = stackalloc byte[pathData.Length];
                pathPtr = new IntPtr(pathDataPtr);
                Marshal.Copy(pathData, 0, pathPtr, pathData.Length);
            }

            INativeData res;
            try
            {
                res = await StoreApi.Api.ExecBlobPrepareWriteAsync(appId, cmdIdPtr, pathPtr, (uint)pathData.Length, needSize, 0);
            }
            catch (RaftGroupNotExistsException)
            {
                Log.Debug("BlobMetaRaftGroup不存在，尝试创建...");
                await TryCreateMetaRaftGroupAsync(appId);
                res = await PrepareWriteAsync(appId, txnId, path, needSize);
            }

            return res;
        }

        private static Guid GenWriteTxnId()
        {
            Guid txnId = Guid.Empty;
            DateTime unixBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixSeconds = (long)(DateTime.UtcNow - unixBase).TotalSeconds;
            unsafe
            {
                //生成事务命令标识
                var idSeq = System.Threading.Interlocked.Increment(ref CmdIdSeq);
                var idPtr = (long*)&txnId;
                idPtr[0] = unixSeconds;
                idPtr[1] = ((long)Runtime.RuntimeContext.PeerId) << 32 | (long)idSeq;
            }
            return txnId;
        }

        private static PrepareWriteResult ParsePrepareResult(INativeData data)
        {
            PrepareWriteResult res = new PrepareWriteResult();
            unsafe
            {
                byte* dataPtr = (byte*)data.DataPtr.ToPointer();
                ulong* raftGroupIdPtr = (ulong*)dataPtr;
                res.ChunkRaftGroupId = *raftGroupIdPtr;
                Guid* pathIdPtr = (Guid*)(dataPtr + 8);
                res.ParentPathId = *pathIdPtr;
            }
            return res;
        }

        /// <summary>
        /// 仅用于不存在App对应的BlobMetaRaftGroup时创建之
        /// </summary>
        private static async ValueTask TryCreateMetaRaftGroupAsync(byte appStoreID)
        {
            var txn = await Transaction.BeginAsync(); //TODO: 移除事务依赖

            byte appId = appStoreID;
            var partInfo = new PartitionInfo();
            IntPtr pkPtr;
            unsafe
            {
                partInfo.Flags = IdUtil.RAFT_TYPE_BLOB_META << IdUtil.RAFTGROUPID_FLAGS_TYPE_OFFSET;
                partInfo.KeyPtr = new IntPtr(&appId);
                partInfo.KeySize = new IntPtr(1);
                pkPtr = new IntPtr(&partInfo);
            }

            await StoreApi.Api.MetaGenPartitionAsync(txn.Handle, pkPtr);
            await txn.CommitAsync();
        }

        private static async ValueTask<INativeData> TryCreateChunkAsync(byte appId, string path, uint needSize)
        {

            var pathData = System.Text.Encoding.UTF8.GetBytes(path);
            IntPtr pathPtr;
            unsafe
            {
                //暂复制路径
                var pathDataPtr = stackalloc byte[pathData.Length];
                pathPtr = new IntPtr(pathDataPtr);
                Marshal.Copy(pathData, 0, pathPtr, pathData.Length);
            }

            return await StoreApi.Api.BlobCreateChunkAsync(appId, pathPtr, (uint)pathData.Length, needSize);
        }
        #endregion

        #region ====Read Methods====
        public static async ValueTask DownloadAsync(byte appId, Stream stream, string fromPath)
        {
            if (!fromPath.StartsWith('/'))
                throw new ArgumentException("Path must start with '/'", nameof(fromPath));
            if (string.IsNullOrEmpty(Path.GetFileName(fromPath)))
                throw new ArgumentException("Path must has file name", nameof(fromPath));

            //1.从BlobMetaRaftGroup读取文件Meta以获取ChunkRaftGroupId
            var fileMeta = await GetFileMetaAsync(appId, fromPath);
            //TODO:缓存文件Meta
            //Log.Debug($"文件信息: RaftGroupId={fileMeta.RaftGroupId} Size={fileMeta.Size}");

            //2.循环读取文件块并写入流
            string fileName = Path.GetFileName(fromPath);
            byte[] fileNameData = System.Text.Encoding.UTF8.GetBytes(fileName);
            int keySize = 9 + fileNameData.Length;
            IntPtr keyPtr = Marshal.AllocHGlobal(keySize);
            unsafe
            {
                byte* readTypePtr = (byte*)keyPtr.ToPointer();
                readTypePtr[0] = (byte)BlobReadType.kGetFileSegment;
                var namePtr = keyPtr + 9;
                Marshal.Copy(fileNameData, 0, namePtr, fileNameData.Length);
            }

            uint totalRead = 0;
            uint curRead = 0;
            uint offset = 0;
            uint fileSize = 0;
            do
            {
                unsafe
                {
                    uint* posPtr = (uint*)(keyPtr + 1).ToPointer();
                    posPtr[0] = offset;
                    posPtr[1] = SegmentSize;
                }
                var segmentData = await StoreApi.Api.ReadIndexByGetAsync(fileMeta.RaftGroupId, keyPtr, (uint)keySize);
                ParseAndWriteToStream(segmentData, ref fileSize, ref curRead, stream);
                segmentData.Dispose();

                totalRead += curRead;
                if (totalRead >= fileSize)
                    break;
                offset += SegmentSize;
            } while (true);

            Marshal.FreeHGlobal(keyPtr);
        }

        private static void ParseAndWriteToStream(INativeData segmentData, ref uint fileSize, ref uint curRead, Stream stream)
        {
            curRead = (uint)segmentData.Size - 4;
            IntPtr dataPtr = segmentData.DataPtr;

            unsafe
            {
                uint* fileSizePtr = (uint*)dataPtr.ToPointer();
                fileSize = *fileSizePtr;
                var span = new ReadOnlySpan<byte>((dataPtr + 4).ToPointer(), (int)curRead);
                stream.Write(span);
            }
        }

        private static async ValueTask<BlobFile> GetFileMetaAsync(byte appId, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            var pathData = System.Text.Encoding.UTF8.GetBytes(path);
            if (pathData.Length > MaxPathSize)
                throw new ArgumentException("Path length out of range", nameof(path));

            IntPtr pathPtr;
            unsafe
            {
                //暂复制路径
                var pathDataPtr = stackalloc byte[pathData.Length + 1];
                pathDataPtr[0] = (byte)BlobReadType.kGetFileMeta;
                pathPtr = new IntPtr(pathDataPtr);
                Marshal.Copy(pathData, 0, pathPtr + 1, pathData.Length);
            }

            ulong metaRaftGroupId = (ulong)appId << IdUtil.RAFTGROUPID_APPID_OFFSET;
            INativeData res = await StoreApi.Api.ReadIndexByGetAsync(metaRaftGroupId, pathPtr, (uint)(pathData.Length + 1));
            unsafe
            {
                BlobFile* srcPtr = (BlobFile*)res.DataPtr.ToPointer();
                return *srcPtr;
            }
        }
        #endregion

        #region ====List Methods====
        /// <summary>
        /// 列出指定目录下的子目录及文件信息
        /// </summary>
        /// <returns>目录不存在返回null</returns>
        /// <param name="appId">App identifier.</param>
        /// <param name="path">必须'/'开头，但不能'/'结尾</param>
        public static async ValueTask<BlobObject[]> ListAsync(byte appId, string path/*, int skip, int take*/)
        {
            //TODO:***暂简单实现，忽略分页
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (!path.StartsWith('/') || (path.Length > 1 && path.EndsWith('/')))
                throw new ArgumentException("路径格式错误", nameof(path));

            var list = new List<BlobObject>();
            ulong metaRaftGroupId = (ulong)appId << IdUtil.RAFTGROUPID_APPID_OFFSET
                | (ulong)IdUtil.RAFT_TYPE_BLOB_META << (IdUtil.RAFTGROUPID_FLAGS_OFFFSET + IdUtil.RAFTGROUPID_FLAGS_TYPE_OFFSET);

            #region ----先查询目录是否存在----
            byte[] pathData = System.Text.Encoding.UTF8.GetBytes(path);
            if (path.Length > 1) //注意: 非根目录替换last '/' 0x2F to 0x00
            {
                for (int i = pathData.Length - 1; i >= 0; i--)
                {
                    if (pathData[i] == 0x2F)
                    {
                        pathData[i] = 0;
                        break;
                    }
                }
            }

            int keySize = pathData.Length + 2;
            IntPtr keyPtr;
            unsafe
            {
                byte* kp = stackalloc byte[keySize];
                kp[0] = KEY_TYPE_PATH;
                kp[1] = appId;
                Marshal.Copy(pathData, 0, new IntPtr(kp + 2), pathData.Length);
                keyPtr = new IntPtr(kp);
            }

            var pathMetaData = await StoreApi.Api.ReadIndexByGetAsync(metaRaftGroupId, keyPtr, (uint)keySize);
            if (pathMetaData == null)
                return null;
            BlobPath curPath = new BlobPath();
            curPath.ReadFrom(pathMetaData.DataPtr);
            pathMetaData.Dispose();
            #endregion

            #region ----查询当前目录下子目录----
            string pathPrefix = path.Length == 1 ? path : path + "/";
            byte[] childPathPrefixData = System.Text.Encoding.UTF8.GetBytes(pathPrefix);
            childPathPrefixData[childPathPrefixData.Length - 1] = 0;  //注意替换last '/' 0x2F to 0x00

            keySize = childPathPrefixData.Length + 2;
            IntPtr beginKeyPtr;
            IntPtr endKeyPtr;
            IntPtr filterPtr = IntPtr.Zero;
            unsafe
            {
                byte* bk = stackalloc byte[keySize];
                bk[0] = KEY_TYPE_PATH; //TypeFlag = Path
                bk[1] = appId;
                Marshal.Copy(childPathPrefixData, 0, new IntPtr(bk + 2), childPathPrefixData.Length);
                beginKeyPtr = new IntPtr(bk);

                byte* ek = stackalloc byte[keySize];
                ek[0] = KEY_TYPE_PATH;
                ek[1] = appId;
                Marshal.Copy(childPathPrefixData, 0, new IntPtr(ek + 2), childPathPrefixData.Length);
                ek[childPathPrefixData.Length + 1] = 1;
                endKeyPtr = new IntPtr(ek);
            }
            //Log.Debug($"扫描子目录BeginKey={StringHelper.ToHexString(beginKeyPtr, keySize)}");
            //Log.Debug($"扫描子目录EndKey={StringHelper.ToHexString(endKeyPtr, keySize)}");

            var req = new ClrScanRequire
            {
                RaftGroupId = metaRaftGroupId,
                BeginKeyPtr = beginKeyPtr,
                BeginKeySize = new IntPtr(keySize),
                EndKeyPtr = endKeyPtr,
                EndKeySize = new IntPtr(keySize),
                FilterPtr = filterPtr,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (scanRes != null)
            {
                scanRes.ForEachRow((kp, ks, vp, vs) =>
                {
                    BlobObject bo = new BlobObject();
                    bo.ReadFrom(kp, ks, vp, vs);
                    list.Add(bo);
                });
                scanRes.Dispose();
            }
            #endregion

            #region ----查询当前目录下文件----
            if (curPath.Chunks != null && curPath.Chunks.Length > 0)
            {
                keySize = 19;
                unsafe
                {
                    byte* bk = stackalloc byte[keySize];
                    bk[0] = KEY_TYPE_FILE;
                    bk[1] = appId;
                    Guid* idPtr = (Guid*)(bk + 2);
                    *idPtr = curPath.Id;
                    bk[18] = 0;
                    beginKeyPtr = new IntPtr(bk);

                    byte* ek = stackalloc byte[keySize];
                    ek[0] = KEY_TYPE_FILE;
                    ek[1] = appId;
                    idPtr = (Guid*)(ek + 2);
                    *idPtr = curPath.Id;
                    ek[18] = 1;
                    endKeyPtr = new IntPtr(ek);
                }

                var req2 = new ClrScanRequire
                {
                    RaftGroupId = metaRaftGroupId,
                    BeginKeyPtr = beginKeyPtr,
                    BeginKeySize = new IntPtr(keySize),
                    EndKeyPtr = endKeyPtr,
                    EndKeySize = new IntPtr(keySize),
                    FilterPtr = filterPtr,
                    Skip = 0,
                    Take = uint.MaxValue,
                    DataCF = -1
                };
                IntPtr reqPtr2;
                unsafe { reqPtr2 = new IntPtr(&req2); }
                scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr2);
                if (scanRes != null)
                {
                    scanRes.ForEachRow((kp, ks, vp, vs) =>
                    {
                        BlobObject bo = new BlobObject();
                        bo.ReadFrom(kp, ks, vp, vs);
                        list.Add(bo);
                    });
                    scanRes.Dispose();
                }
            }
            #endregion

            return list.ToArray();
        }
        #endregion
    }

    #region ====Structs & Enums====
    /// <summary>
    /// 包装BlobPath or BlobFile，用于传至前端
    /// </summary>
    public struct BlobObject : IJsonSerializable
    {
        public string Name;
        public int Size;
        public DateTime CreateTime;
        public DateTime ModifiedTime;
        public bool IsFile;

        #region ----Json----
        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(Name));
            writer.WriteValue(Name);

            writer.WritePropertyName(nameof(Size));
            writer.WriteValue(Size);

            writer.WritePropertyName(nameof(CreateTime));
            writer.WriteValue(CreateTime);

            writer.WritePropertyName(nameof(ModifiedTime));
            writer.WriteValue(ModifiedTime);

            writer.WritePropertyName(nameof(IsFile));
            writer.WriteValue(IsFile);
        }
        #endregion

        internal unsafe void ReadFrom(IntPtr kp, int ks, IntPtr vp, int vs)
        {
            //Log.Debug($"Key = {StringHelper.ToHexString(kv.KeyPtr, kv.KeySize.ToInt32())}");
            //Log.Debug($"Value = {StringHelper.ToHexString(kv.ValuePtr, kv.ValueSize.ToInt32())}");

            byte* keyPtr = (byte*)kp.ToPointer();
            int keySize = ks - 2;
            if (keyPtr[0] == BlobStore.KEY_TYPE_PATH)
            {
                IsFile = false;
                var keySpan = new ReadOnlySpan<byte>(keyPtr + 2, keySize);
                int sepIndex = keySpan.LastIndexOf((byte)0) + 1;
                Name = new string((sbyte*)(keyPtr + 2), sepIndex, keySize - sepIndex, System.Text.Encoding.UTF8);

                BlobPath path = new BlobPath();
                path.ReadFrom(vp);
                Size = (int)path.GetChunksUsedSize();
            }
            else if (keyPtr[0] == BlobStore.KEY_TYPE_FILE)
            {
                IsFile = true;
                Name = new string((sbyte*)(keyPtr + 19), 0, keySize - 17, System.Text.Encoding.UTF8);

                BlobFile* filePtr = (BlobFile*)vp.ToPointer();
                Size = (int)filePtr->Size;
            }
            else
            {
                throw new Exception("Unkonwn key type");
            }
        }
    }

    struct PrepareWriteResult
    {
        public ulong ChunkRaftGroupId;
        public Guid ParentPathId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BlobChunkInfo
    {
        public ulong RaftGroupId;
        public uint UsedSize;
    }

    struct BlobPath
    {
        public Guid Id;
        public bool Deleting;
        public BlobChunkInfo[] Chunks;

        internal long GetChunksUsedSize()
        {
            if (Chunks == null || Chunks.Length == 0)
                return 0;
            return Chunks.Sum(t => t.UsedSize);
        }

        internal unsafe void ReadFrom(IntPtr dataPtr)
        {
            byte* dp = (byte*)dataPtr.ToPointer();
            Guid* idPtr = (Guid*)dp;
            Id = *idPtr;
            Deleting = dp[16] > 0;
            uint* chunkCountPtr = (uint*)(dp + 17);
            var chunkCount = *chunkCountPtr;
            if (chunkCount > 0)
            {
                Chunks = new BlobChunkInfo[chunkCount];
                BlobChunkInfo* chunkDataPtr = null;
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkDataPtr = (BlobChunkInfo*)(dp + 21 + i * 12);
                    Chunks[i] = *chunkDataPtr;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BlobFile
    {
        public ulong RaftGroupId;
        public uint Size;
        public bool Deleting;
    }

    enum BlobReadType : byte
    {
        kNone = 0,
        kGetFileSegment = 1,
        kGetFileMeta = 10,
    };
    #endregion
}
