#if FUTURE

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using appbox.Caching;
using appbox.Data;
using appbox.Runtime;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 适用于Host进程的存储异步Api，直接调用NativeApi并处理NativeMessage回复
    /// </summary>
    sealed class HostStoreApi : IStoreApi
    {
        private readonly ObjectPool<PooledTaskSource<NativeMessage>> taskPool =
            PooledTaskSource<NativeMessage>.Create(256); //TODO: check count

        #region ====Meta====
        /// <summary>
        /// Creates the application async.
        /// </summary>
        /// <param name="keyPtr">Caller需要释放分配的内存</param>
        /// <param name="dataPtr">Caller不需要释放分配的内存</param>
        internal async ValueTask<byte> CreateApplicationAsync(IntPtr keyPtr, uint keySize, IntPtr dataPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.CreateApplication(ts.GCHandlePtr, keyPtr, keySize, dataPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            //TODO:异常处理
            return (byte)msg.Data1.ToInt32();
        }

        public async ValueTask<ulong> MetaGenPartitionAsync(IntPtr txnPtr, IntPtr partionInfoPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.MetaGenPartition(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, partionInfoPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            //TODO:异常处理
            return (ulong)msg.Data1.ToInt64();
        }

        /// <summary>
        /// ProposeConfChange
        /// </summary>
        internal async ValueTask<bool> ProposeConfChangeAsync(byte type, ulong nodeId, uint ip, ushort port)
        {
            var ts = taskPool.Allocate();
            NativeApi.ProposeConfChange(ts.GCHandlePtr, type, nodeId, ip, port);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            return msg.Data1 == new IntPtr(1);
        }

        /// <summary>
        /// 提升集群复制因子
        /// </summary>
        /// <param name="factor">3 or 5 or 7</param>
        internal async ValueTask PromoteReplFactorAsync(byte factor)
        {
            var ts = taskPool.Allocate();
            NativeApi.PromoteReplFactor(ts.GCHandlePtr, factor);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            if (msg.Data1 != IntPtr.Zero)
                throw new Exception($"ErrorCode: {(KVCommandError)msg.Data1.ToInt32()}");
        }

        /// <summary>
        /// Generate ModelId async.
        /// </summary>
        internal async ValueTask<uint> MetaGenModelIdAsync(uint appId, bool devLayer)
        {
            var ts = taskPool.Allocate();
            NativeApi.GenModelId(ts.GCHandlePtr, appId, devLayer);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            if (msg.Data1 == IntPtr.Zero)
                return (uint)msg.Data2.ToInt32();

            throw new Exception($"ErrorCode: {msg.Data1.ToInt32()}");
        }

        internal async ValueTask ExecMetaAlterTableAsync(IntPtr txnPtr, IntPtr cmdPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecMetaAlterTable(txnPtr, cmdPtr, ts.GCHandlePtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
                return;

            throw new Exception($"MetaAlterTable error: {errorCode}");
        }

        internal async ValueTask ExecMetaDropTableAsync(IntPtr txnPtr, uint tableId, ulong modelId,
            IntPtr partsPtr, IntPtr partsSize, bool truncate)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecMetaDropTable(txnPtr, ts.GCHandlePtr, tableId, modelId, partsPtr, partsSize, truncate);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
                return;

            throw new Exception($"MetaDropTable error: {errorCode}");
        }
        #endregion

        #region ====Transaction====
        public async ValueTask<IntPtr> BeginTransactionAsync(bool readCommitted)
        {
            var ts = taskPool.Allocate();
            NativeApi.BeginTransaction(readCommitted, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            //TODO:异常处理
            return msg.Data1;
        }

        public async ValueTask CommitTransactionAsync(IntPtr txnPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.CommitTransaction(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            if (msg.Data1 == IntPtr.Zero)
                return;
            throw new Exception($"Commit error: {msg.Data1.ToInt32()}");
        }

        public void RollbackTransaction(IntPtr txnPtr, bool isAbort)
        {
            NativeApi.RollbackTransaction(txnPtr, isAbort);
        }
        #endregion

        #region ====KV====
        public async ValueTask ExecKVInsertAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecKVInsert(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, reqPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
                return;

            throw new Exception($"Insert error: {errorCode}");
        }

        public async ValueTask<INativeData> ExecKVUpdateAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecKVUpdate(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, reqPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeString(msg.Data2);
            }

            throw new Exception($"Update error: {errorCode}");
        }

        public async ValueTask<INativeData> ExecKVDeleteAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecKVDelete(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, reqPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeString(msg.Data2);
            }
            throw new Exception($"Delete error:{errorCode}");
        }

        public async ValueTask ExecKVAddRefAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecKVAddRef(txnPtr, ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, reqPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
                return;

            throw new Exception($"AddRef error: {errorCode}");
        }
        #endregion

        #region ====Read====
        public async ValueTask<INativeData> ReadIndexByGetAsync(ulong raftGroupId, IntPtr keyPtr, uint keySize, int dataCF = -1)
        {
            var ts = taskPool.Allocate();
            NativeApi.ReadIndexByGet(ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, raftGroupId, keyPtr, keySize, dataCF);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = msg.Data1.ToInt32();
            if (errorCode == 0)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeString(msg.Data2);
            }
            if (errorCode == 999) //TODO: fix errocode
            {
                throw RaftGroupNotExistsException.Default;
            }

            throw new Exception($"Get error:{errorCode}");
        }

        public async ValueTask<IScanResponse> ReadIndexByScanAsync(IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.ReadIndexByScan(ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, reqPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeScanResponse(msg.Data2, msg.Data3.ToInt32());
            }
            throw new Exception($"Scan error:{errorCode}");
        }
        #endregion

        #region ====ScanByClr====
        [StructLayout(LayoutKind.Sequential)]
        internal struct ScanReqInfo
        {
            internal uint Skip;
            internal uint Take;
            internal IntPtr FilterPtr;
            internal IntPtr FilterSize;
            internal ulong Timestamp;
            internal bool IsMvcc;
        }

        private static readonly LRUCache<BytesKey, KVFilterFunc> filters =
            new LRUCache<BytesKey, KVFilterFunc>(1024, BytesKeyEqualityComparer.Default);

        /// <summary>
        /// 处理Native委托Clr执行过滤扫描
        /// </summary>
        internal static unsafe void ScanByClr(IntPtr msgPtr)
        {
            NativeMessage* msg = (NativeMessage*)msgPtr;
            var shard = msg->Shard;
            var handle = msg->Handle;
            var it = msg->Data1;
            var req = msg->Data2;
            var res = msg->Data3;

            var queueOk = ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                //注意：需要处理异常
                //TODO:全局索引过滤特殊处理,本地索引可以通过Api直接读目标数据，但全局索引可考虑按批加载目标后再过滤

                IteratorKV itkv = new IteratorKV();
                var itkvPtr = new IntPtr(&itkv);
                var reqInfo = new ScanReqInfo();
                NativeApi.ScanGetReqInfo(req, new IntPtr(&reqInfo), it, itkvPtr); //注意同时填充第一条IteratorKV

                //处理过滤条件，注意：暂使用缓存方案，防止反复Expression.Compile(性能损耗太大)
                Debug.Assert(reqInfo.FilterPtr != IntPtr.Zero && reqInfo.FilterSize != IntPtr.Zero);
                BytesKey key = new BytesKey(reqInfo.FilterPtr, reqInfo.FilterSize.ToInt32());
                if (!filters.TryGet(key, out KVFilterFunc filterFunc))
                {
                    try
                    {
                        var filter = (Expressions.Expression)ModelStore.DeserializeModel(reqInfo.FilterPtr, reqInfo.FilterSize.ToInt32());
                        var body = filter.ToLinqExpression(KVScanExpressionContext.Default); //TODO:参考FastExpressionCompiler直接编译
                        var exp = Expression.Lambda<KVFilterFunc>(body,
                            KVScanExpressionContext.Default.GetParameter("vp"),
                            KVScanExpressionContext.Default.GetParameter("vs"),
                            KVScanExpressionContext.Default.GetParameter("mv"),
                            KVScanExpressionContext.Default.GetParameter("ts")
                            );
                        //Expression<Func<KVTuple, bool>> exp = t => t.StringEquals(Consts.EMPLOEE_NAME_ID, testData); //测试过滤器2
                        //Expression<KVFilterFunc> exp = (vp, vs, mvcc, ts) => false;

                        filterFunc = exp.Compile(); //TODO:待进一步研究 exp.CompileFast();
                        filters.TryAdd(key.CopyToManaged(), filterFunc);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Compile filter expression error: Type: {ex.GetType()}\nMessage:{ex.Message}\nStack:{ex.StackTrace}");
                        NativeApi.ScanFinished(shard, handle, it, res, (uint)KVCommandError.ClrCompileFilterFailed);
                        return;
                    }
                }

                //int count = 0;
                //var sw = new Stopwatch();
                //long times = 0;
                //sw.Start();

                uint skipped = 0;
                uint taken = 0;
                int itStatus = 0;
                while (true) //TODO:迭代过程异常处理
                {
                    //打印KV调试信息
                    //count++;
                    //var keyString = StringHelper.ToHexString(itkv.KeyPtr, itkv.KeySize.ToInt32());
                    //var valueString = StringHelper.ToHexString(itkv.ValuePtr, itkv.ValueSize.ToInt32());
                    //Log.Debug($"Key={keyString} Value={valueString}");

                    //开始过滤表达式判断
                    if (filterFunc(itkv.ValuePtr, itkv.ValueSize.ToInt32(), reqInfo.IsMvcc, reqInfo.Timestamp)) //50-60 -> 20
                    {
                        if (skipped < reqInfo.Skip)
                        {
                            skipped++;
                        }
                        else
                        {
                            NativeApi.ScanResponseAddKV(res, it, reqInfo.IsMvcc, reqInfo.Timestamp);
                            taken++;
                            if (taken >= reqInfo.Take)
                            {
                                break;
                            }
                        }
                    }

                    itStatus = NativeApi.ScanNextValidIterator(it, req, itkvPtr); //未优化前350-400 -> 95
                    if (itStatus == -1) //读到已被GC的记录
                    {
                        NativeApi.ScanFinished(shard, handle, it, res, (uint)KVCommandError.ReadGCData);
                        return;
                    }
                    if (itStatus == 0)
                    {
                        break;
                    }
                } //end while

                //sw.Stop();
                //times += sw.ElapsedTicks;
                //Console.WriteLine($"Thread:{Thread.CurrentThread.ManagedThreadId} Scan:{count} 耗时:{sw.ElapsedMilliseconds}");
                //Console.WriteLine($"Scan:{count} 步骤耗时:{times / TimeSpan.TicksPerMillisecond}");
                NativeApi.ScanResponseSetSkipped(res, skipped); //TODO:移至ScanFinished参数
                NativeApi.ScanFinished(shard, handle, it, res, 0);
            }, null);

            if (!queueOk)
            {
                Log.Warn("Cannot queue scan task");
                NativeApi.ScanFinished(shard, handle, it, res, (uint)KVCommandError.ClrEnqueueTaskFailed);
            }
        }
        #endregion

        #region ====Blob====
        public async ValueTask<INativeData> ExecBlobPrepareWriteAsync(byte appId, IntPtr cmdIdPtr,
                                           IntPtr pathPtr, uint pathSize, uint size, uint option)
        {
            var ts = taskPool.Allocate();
            NativeApi.ExecBlobPrepareWrite(ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, appId, cmdIdPtr, pathPtr, pathSize, size, option);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = msg.Data1.ToInt32();
            if (errorCode == 0)
                return msg.Data2 == IntPtr.Zero ? null : new NativeString(msg.Data2);
            if (errorCode == 7)
                throw RaftGroupNotExistsException.Default;
            throw new Exception($"BlobPrepareWrite error: {errorCode}");
        }

        public async ValueTask<INativeData> BlobCreateChunkAsync(byte appId, IntPtr pathPtr, uint pathSize, uint needSize)
        {
            var ts = taskPool.Allocate();
            NativeApi.BlobCreateChunk(ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, appId, pathPtr, pathSize, needSize);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = msg.Data1.ToInt32();
            if (errorCode == 0)
                return msg.Data2 == IntPtr.Zero ? null : new NativeString(msg.Data2);

            throw new Exception($"BlobPrepareWrite error: {errorCode}");
        }

        public async ValueTask BlobWriteChunkAsync(ulong raftGroupId, IntPtr pathPtr, uint pathSize, uint option, IntPtr dataPtr)
        {
            var ts = taskPool.Allocate();
            NativeApi.BlobWriteChunk(ts.GCHandlePtr, RuntimeContext.Current.RuntimeId, raftGroupId, pathPtr, pathSize, option, dataPtr);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = msg.Data1.ToInt32();
            if (errorCode != 0)
                throw new Exception($"BlobWriteChunk error: {errorCode}");
        }
        #endregion
    }
}

#endif