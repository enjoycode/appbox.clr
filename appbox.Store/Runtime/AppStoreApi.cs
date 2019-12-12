using System;
using System.Threading.Tasks;
using appbox.Caching;
using appbox.Runtime;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 适用于应用或调试子进程的存储异步Api，发送相应的请求消息至Host进程调用NativeApi然后接收转换过的NativeMessage处理回复
    /// </summary>
    sealed class AppStoreApi : IStoreApi
    {
        private readonly IMessageChannel channel;
        private readonly ObjectPool<PooledTaskSource<NativeMessage>> taskPool
            = PooledTaskSource<NativeMessage>.Create(256); //TODO: check count

        internal AppStoreApi(IMessageChannel channel)
        {
            this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        #region ====Meta====
        public async ValueTask<ulong> MetaGenPartitionAsync(IntPtr txnPtr, IntPtr partionInfoPtr)
        {
            var ts = taskPool.Allocate();
            var req = new GenPartitionRequire(txnPtr, ts.GCHandlePtr, partionInfoPtr);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            //TODO:异常处理
            return (ulong)msg.Data1.ToInt64();
        }
        #endregion

        #region ====Transaction====
        public async ValueTask<IntPtr> BeginTransactionAsync(bool readCommitted)
        {
            var ts = taskPool.Allocate();
            var req = new BeginTranRequire(readCommitted, ts.GCHandlePtr);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            //TODO:异常处理
            return msg.Data1;
        }

        public async ValueTask CommitTransactionAsync(IntPtr txnPtr)
        {
            var ts = taskPool.Allocate();
            var req = new CommitTranRequire(txnPtr, ts.GCHandlePtr);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            if (msg.Data1 == IntPtr.Zero)
                return;
            throw new Exception($"Commit error: {msg.Data1.ToInt32()}");
        }

        public void RollbackTransaction(IntPtr txnPtr, bool isAbort)
        {
            var req = new RollbackTranRequire(txnPtr, isAbort);
            channel.SendMessage(ref req);
        }
        #endregion

        #region ====KV====
        public async ValueTask ExecKVInsertAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            var req = new KVInsertRequire(ts.GCHandlePtr, txnPtr, reqPtr);
            channel.SendMessage(ref req);
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
            var req = new KVUpdateRequire(ts.GCHandlePtr, txnPtr, reqPtr);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeBytes(msg.Data2);
            }

            throw new Exception($"Update error: {errorCode}");
        }

        public async ValueTask<INativeData> ExecKVDeleteAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            var req = new KVDeleteRequire(ts.GCHandlePtr, txnPtr, reqPtr);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeBytes(msg.Data2);
            }

            throw new Exception($"Delete error: {errorCode}");
        }

        public async ValueTask ExecKVAddRefAsync(IntPtr txnPtr, IntPtr reqPtr)
        {
            var ts = taskPool.Allocate();
            var req = new KVAddRefRequire(ts.GCHandlePtr, txnPtr, reqPtr);
            channel.SendMessage(ref req);
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
            var req = new KVGetRequire(ts.GCHandlePtr, raftGroupId, dataCF, keyPtr, keySize);
            channel.SendMessage(ref req);
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = msg.Data1.ToInt32();
            if (errorCode == 0)
            {
                return msg.Data2 == IntPtr.Zero ? null : new NativeBytes(msg.Data2);
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
            var req = new KVScanRequire(ts.GCHandlePtr, reqPtr);
            channel.SendMessage(ref req);
            req.FreeFilterData(); //注意释放
            var msg = await ts.WaitAsync();
            taskPool.Free(ts);
            var errorCode = (KVCommandError)msg.Data1.ToInt32();
            if (errorCode == KVCommandError.None)
            {
                return msg.Data2 == IntPtr.Zero ? null : new RemoteScanResponse(msg.Data2, msg.Data3.ToInt32());
            }
            throw new Exception($"Scan error:{errorCode}");
        }
        #endregion

        #region ====Blob====
        public ValueTask<INativeData> ExecBlobPrepareWriteAsync(byte appId, IntPtr cmdIdPtr, IntPtr pathPtr, uint pathSize, uint size, uint option)
        {
            throw ExceptionHelper.NotImplemented();
        }

        public ValueTask<INativeData> BlobCreateChunkAsync(byte appId, IntPtr pathPtr, uint pathSize, uint needSize)
        {
            throw ExceptionHelper.NotImplemented();
        }

        public ValueTask BlobWriteChunkAsync(ulong raftGroupId, IntPtr pathPtr, uint pathSize, uint option, IntPtr dataPtr)
        {
            throw ExceptionHelper.NotImplemented();
        }
        #endregion
    }
}
