using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using appbox.Server;

namespace appbox.Store
{
    public static class StoreApi
    {
        internal static IStoreApi Api;

        internal static void Init(IStoreApi api)
        {
            Api = api ?? throw new ArgumentNullException(nameof(api));
        }
    }

    /// <summary>
    /// 用于抽象主进程与子进程的存储Api
    /// </summary>
    interface IStoreApi
    {
        ValueTask<IntPtr> BeginTransactionAsync(bool readCommitted);
        ValueTask CommitTransactionAsync(IntPtr txnPtr);
        void RollbackTransaction(IntPtr txnPtr, bool isAbort);

        ValueTask<ulong> MetaGenPartitionAsync(IntPtr txnPtr, IntPtr partionInfoPtr);

        /// <summary>
        /// Exec KVInsert async
        /// </summary>
        /// <remarks>
        /// req.KeyPtr  Caller需要释放分配的内存
        /// req.DataPtr Caller不需要释放分配的内存
        /// </remarks>
        ValueTask ExecKVInsertAsync(IntPtr txnPtr, IntPtr reqPtr);

        /// <summary>
        /// Exec KVUpdate async
        /// </summary>
        /// <remarks>
        /// req.KeyPtr  Caller需要释放分配的内存
        /// req.DataPtr Caller不需要释放分配的内存
        /// </remarks>
        ValueTask<INativeData> ExecKVUpdateAsync(IntPtr txnPtr, IntPtr reqPtr);

        /// <summary>
        /// Exec KVInsert async
        /// </summary>
        /// <remarks>
        /// req.KeyPtr  Caller需要释放分配的内存
        /// </remarks>
        ValueTask<INativeData> ExecKVDeleteAsync(IntPtr txnPtr, IntPtr reqPtr);

        ValueTask ExecKVAddRefAsync(IntPtr txnPtr, IntPtr reqPtr);

        ValueTask<INativeData> ReadIndexByGetAsync(ulong raftGroupId, IntPtr keyPtr, uint keySize, int dataCF = -1);

        /// <summary>
        /// ReadIndexByScanAsync
        /// </summary>
        /// <remarks>
        /// req.KeyPtr  Caller需要释放分配的内存
        /// req.FilterPtr Caller不需要释放分配的内存
        /// </remarks>
        /// <returns>可能返回null</returns>
        ValueTask<IScanResponse> ReadIndexByScanAsync(IntPtr reqPtr);

        ValueTask<INativeData> ExecBlobPrepareWriteAsync(byte appId, IntPtr cmdIdPtr,
                                           IntPtr pathPtr, uint pathSize, uint size, uint option);

        ValueTask<INativeData> BlobCreateChunkAsync(byte appId, IntPtr pathPtr, uint pathSize, uint needSize);

        ValueTask BlobWriteChunkAsync(ulong raftGroupId, IntPtr pathPtr, uint pathSize, uint option, IntPtr dataPtr);
    }

    enum KVCommandError //同Native一致
    {
        None = 0,
        WaitForOther = 1,               //排入队列等待其他事务递交或回滚
        InsertKeyConflict = 2,          //插入时键已存在
        UpdateNotExists = 3,
        CommitTargetNotExists = 4,      //递交时未找到目标命令
        ProposeDropped = 5,
        SerializeError = 6,
        ClrCompileFilterFailed = 7,     //转换编译条件表达式错误
        ClrEnqueueTaskFailed = 8,       //无法加入Clr线程池
        ApplicationNotExists = 9,       //GenModelId时无法找到Application
        SaveModelIdCounterFailed = 10,  //保存Application的ModelId计数器失败
        RocksDBGetFailed = 11,          //RocksDB获取值错误
        RocksDBPutFailed = 20,          //RocksDB写值错误
        AlterTableHasNoChange = 12,     //AlterTable没有变更项
        AlterTableHasOldTask = 13,      //MetaAlterTable存在旧任务未完成
        AlterPartionRepeated = 14,      //重复提交任务至分区
        AlterBatchHasOld = 15,          //已存在旧的批量更新
        ProposeToNotExistsRaftGroup = 16, //提议至不存在的RaftGroup
        ScanTakeNone = 17,              //扫描时Take参数等于0
        GetTableSchemaVersionError = 18,//读取PartCF的SchemaVersion错误
        TableSchemaChanged = 19,        //表结构已经变更
        AppStoreIdInvalid = 21,         //无效标识
        ReadGCData = 22,                //读到已经被GC的数据
        RefKeyNotExists = 23,           //引用外键不存在
        ForeignKeyConstraint = 24,      //删除时有引用外键存在
        LockFailed = 25,                //状态机上锁时死锁或其他错误
        IndexTargetSame = 26,
        IndexNotReady = 27,             //索引在构建中或构建失败
    }

    public sealed class RaftGroupNotExistsException : Exception
    {
        public static readonly RaftGroupNotExistsException Default = new RaftGroupNotExistsException();

        private RaftGroupNotExistsException() { }
    }
}
