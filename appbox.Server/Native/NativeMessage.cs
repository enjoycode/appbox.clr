#if FUTURE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{

    public delegate void NativeMessageHandler(IntPtr msgPtr);

    /// <summary>
    /// Native层与Clr层交互的消息，另作为存储引擎请求响应转发至子进程
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeMessage : IMessage
    {
        public uint Type;
        public uint Shard; //Type=EntityStoreCB时表示响应类型: eg:BeginTranResponse, KVInsertResponse等
        public IntPtr Handle; //C->Clr 指向Promise; Clr->C 指向TaskCompletionSource
        public IntPtr Data1;
        public IntPtr Data2;
        public IntPtr Data3;
        public IntPtr Data4;
        public ulong Source; //(低32位表示类型)0=Host进程, 1=App进程, 2=Debug进程(高32位表示Debug会话标识)

        public PayloadType PayloadType => PayloadType.NativeMessage;
        MessageType IMessage.Type => MessageType.NativeMessage;
        private const uint NATIVEMESSAGE_STORECB = 2;

        /// <summary>
        /// 用于转发至子进程后释放Native层分配的内存
        /// </summary>
        internal void FreeData()
        {
            Debug.Assert(Type == NATIVEMESSAGE_STORECB);
            var resType = (StoreCBType)Shard;
            switch (resType)
            {
                case StoreCBType.BeginTransactionCB:
                case StoreCBType.CommitTransactionCB:
                case StoreCBType.MetaGenPartitionCB:
                case StoreCBType.ExecKVInsertCB:
                case StoreCBType.ExecKVAddRefCB:
                    break;
                case StoreCBType.ExecKVUpdateCB: //TODO:暂与下相同
                case StoreCBType.ExecKVDeleteCB: //TODO:暂与下相同
                case StoreCBType.ReadIndexByGetCB:
                    if (Data1 == IntPtr.Zero && Data2 != IntPtr.Zero)
                        NativeApi.FreeNativeString(Data2);
                    break;
                case StoreCBType.ReadIndexByScanCB:
                    if (Data1 == IntPtr.Zero && Data2 != IntPtr.Zero)
                        NativeApi.FreeScanResponse(Data2);
                    break;
                default:
                    throw new NotImplementedException(resType.ToString());
            }
        }

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            Debug.Assert(Type == NATIVEMESSAGE_STORECB);
            bs.Write(Type);
            bs.Write(Shard);
            bs.Write(Handle.ToInt64());
            //根据类型进行不同的序列化
            var resType = (StoreCBType)Shard;
            switch (resType)
            {
                case StoreCBType.ExecKVUpdateCB: //TODO:暂以下相同
                case StoreCBType.ExecKVDeleteCB: //TODO:暂以下相同
                case StoreCBType.ReadIndexByGetCB: WriteReadIndexByGetRes(bs); break;
                case StoreCBType.ReadIndexByScanCB: WriteReadIndexByScanRes(bs); break;

                case StoreCBType.MetaGenPartitionCB:
                case StoreCBType.BeginTransactionCB: bs.Write(Data1.ToInt64()); bs.Write(Data2.ToInt64()); break;

                case StoreCBType.ExecKVInsertCB:
                case StoreCBType.ExecKVAddRefCB:
                case StoreCBType.CommitTransactionCB: bs.Write(Data1.ToInt64()); break;
                default:
                    throw new NotImplementedException(resType.ToString());
            }
        }

        public void ReadObject(BinSerializer bs)
        {
            Type = bs.ReadUInt32();
            Shard = bs.ReadUInt32();
            Handle = new IntPtr(bs.ReadInt64());
            var resType = (StoreCBType)Shard;
            switch (resType)
            {
                case StoreCBType.ExecKVUpdateCB: //TODO:暂以下相同
                case StoreCBType.ExecKVDeleteCB: //TODO:暂以下相同
                case StoreCBType.ReadIndexByGetCB: ReadReadIndexByGetRes(bs); break;
                case StoreCBType.ReadIndexByScanCB: ReadReadIndexByScanRes(bs); break;

                case StoreCBType.MetaGenPartitionCB:
                case StoreCBType.BeginTransactionCB: Data1 = new IntPtr(bs.ReadInt64()); Data2 = new IntPtr(bs.ReadInt64()); break;

                case StoreCBType.ExecKVInsertCB:
                case StoreCBType.ExecKVAddRefCB:
                case StoreCBType.CommitTransactionCB: Data1 = new IntPtr(bs.ReadInt64()); break;
                default:
                    throw new NotImplementedException(resType.ToString());
            }
        }

        private unsafe void WriteReadIndexByGetRes(BinSerializer bs)
        {
            bs.Write(Data1.ToInt64());
            if (Data1 == IntPtr.Zero && Data2 != IntPtr.Zero) //注意: Data2可能为0
            {
                var size = (int)NativeApi.GetStringSize(Data2);
                var dataPtr = NativeApi.GetStringData(Data2);
                var dataSpan = new ReadOnlySpan<byte>(dataPtr.ToPointer(), size);
                bs.Write(size);
                bs.Stream.Write(dataSpan);
            }
            else
            {
                bs.Write(0);
            }
        }

        private unsafe void ReadReadIndexByGetRes(BinSerializer bs)
        {
            Data1 = new IntPtr(bs.ReadInt64());
            int size = bs.ReadInt32();
            if (size > 0)
            {
                IntPtr rawPtr = NativeBytes.MakeRaw(size);
                var dataSpan = new Span<byte>((rawPtr + 4).ToPointer(), size);
                bs.Stream.Read(dataSpan);
                Data2 = rawPtr;
            }
        }

        private unsafe void WriteReadIndexByScanRes(BinSerializer bs)
        {
            bs.Write(Data1.ToInt64()); //error code
            bs.Write(Data3.ToInt64()); //length
            if (Data1 == IntPtr.Zero && Data2 != IntPtr.Zero) //不要判断length，可能包含其他如skipped数据
            {
                int dataSize = 4; //初始化4字节for skipped
                int length = Data3.ToInt32();
                //先计算并写入数据部分大小，方便子进程分配内存(另考虑子进程内使用ByteBuffer缓存分块)
                IteratorKV kv = new IteratorKV();
                IntPtr kvPtr = new IntPtr(&kv);
                for (int i = 0; i < length; i++) //TODO:底层Api实现一次调用计算出总大小
                {
                    NativeApi.ScanResponseGetKV(Data2, i, kvPtr);
                    dataSize += 4 + kv.KeySize.ToInt32() + 4 + kv.ValueSize.ToInt32();
                }
                bs.Write(dataSize);
                //再写入skipped
                uint skipped = NativeApi.ScanResponseGetSkipped(Data2);
                var span = new ReadOnlySpan<byte>(&skipped, 4);
                bs.Stream.Write(span);
                //最后写入记录数据
                int tempSize = 0;
                for (int i = 0; i < length; i++)
                {
                    NativeApi.ScanResponseGetKV(Data2, i, kvPtr);
                    tempSize = kv.KeySize.ToInt32();
                    span = new ReadOnlySpan<byte>(&tempSize, 4);
                    bs.Stream.Write(span);
                    span = new ReadOnlySpan<byte>(kv.KeyPtr.ToPointer(), tempSize);
                    bs.Stream.Write(span);

                    tempSize = kv.ValueSize.ToInt32();
                    span = new ReadOnlySpan<byte>(&tempSize, 4);
                    bs.Stream.Write(span);
                    span = new ReadOnlySpan<byte>(kv.ValuePtr.ToPointer(), tempSize);
                    bs.Stream.Write(span);
                }
            }
            else
            {
                bs.Write(0); //dataSize
            }
        }

        private unsafe void ReadReadIndexByScanRes(BinSerializer bs)
        {
            Data1 = new IntPtr(bs.ReadInt64());
            Data3 = new IntPtr(bs.ReadInt64());
            int dataSize = bs.ReadInt32();
            if (dataSize > 0)
            {
                IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);
                var span = new Span<byte>(dataPtr.ToPointer(), dataSize);
                bs.Stream.Read(span);
                Data2 = dataPtr;
            }
        }
        #endregion
    }

    enum StoreCBType : uint
    {
        BeginTransactionCB = 0,
        CommitTransactionCB,
        CreateApplicationCB,
        ExecKVDeleteCB,
        ExecKVInsertCB,
        ExecKVUpdateCB,
        ExecKVAddRefCB,
        ExecMetaAlterTableCB,
        ExecMetaDropTableCB,
        GenModelIdCB,
        MetaPromoteReplFactorCB,
        MetaGenPartitionCB,
        ReadIndexByGetCB,
        ReadIndexByScanCB,
        BlobPrepareWriteCB,
        BlobCreateChunkCB,
        BlobWriteChunkCB,
        ProposeConfChangeCB,
    }

}
#endif