using System;
using System.Runtime.InteropServices;
using System.Security;

namespace appbox.Server
{
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void InvokeClrCB(uint shard, IntPtr promise);

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr GetPeerConfigData();

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ProposeConfChange(IntPtr waitHandle, byte type, ulong nodeId, uint ip, ushort port);

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PromoteReplFactor(IntPtr waitHandle, byte factor);

    //NativeString
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate IntPtr NewNativeString(long size, out byte* dataPtr);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ulong GetStringSize(IntPtr handle);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr GetStringData(IntPtr handle);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FreeNativeString(IntPtr handle);

    //Meta
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CreateApplication(IntPtr waitHandle, IntPtr keyPtr, uint keySize, IntPtr dataPtr);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void GenModelId(IntPtr waitHandle, uint appId, bool devLayer);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void MetaGenPartition(IntPtr txnPtr, IntPtr waitHandle, ulong source, IntPtr partionInfoPtr);

    //AlterTable
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr NewAlterTable(uint tableId, uint newSchemaVersion);
    /// <summary>
    /// Alters the table add column.
    /// </summary>
    /// <param name="cmdPtr">Cmd ptr.</param>
    /// <param name="dataPtr">2字节mid + n字节数据部分，存储格式</param>
    /// <param name="dataSize">Data size.</param>
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AlterTableAddColumn(IntPtr cmdPtr, IntPtr dataPtr, int dataSize);
    /// <summary>
    /// Alters the table drop column.
    /// </summary>
    /// <param name="cmdPtr">Cmd ptr.</param>
    /// <param name="memberId">注意非存储格式</param>
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AlterTableDropColumn(IntPtr cmdPtr, ushort memberId);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AlterTableAddIndex(IntPtr cmdPtr, byte indexId, bool global,
        IntPtr fieldsPtr, int fieldsSize, IntPtr storingPtr, int storingSize);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AlterTableDropIndex(IntPtr cmdPtr, byte indexId);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecMetaAlterTable(IntPtr txnPtr, IntPtr cmdPtr, IntPtr waitHandle);

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecMetaDropTable(IntPtr txnPtr, IntPtr waitHandle,
        uint tableId, ulong modelId, IntPtr partsPtr, IntPtr partsSize, bool truncate);

    //Transaction
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BeginTransaction(bool readCommitted, IntPtr waitHandle, ulong source);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CommitTransaction(IntPtr txnPtr, IntPtr waitHandle, ulong source);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void RollbackTransaction(IntPtr txnPtr, bool isAbort);

    //CRUD
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecKVInsert(IntPtr txnPtr, IntPtr waitHandle, ulong source, IntPtr reqPtr);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecKVUpdate(IntPtr txnPtr, IntPtr waitHandle, ulong source, IntPtr reqPtr);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecKVDelete(IntPtr txnPtr, IntPtr waitHandle, ulong source, IntPtr reqPtr);

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecKVAddRef(IntPtr txnPtr, IntPtr waitHandle, ulong source, IntPtr reqPtr);

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ReadIndexByGet(IntPtr waitHandle, ulong source, ulong raftGroupId, IntPtr keyPtr, uint keySize, int dataCF);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ReadIndexByScan(IntPtr waitHandle, ulong source, IntPtr reqPtr);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool ScanGetReqInfo(IntPtr scanReq, IntPtr reqInfo, IntPtr it, IntPtr kv);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ScanNextValidIterator(IntPtr it, IntPtr scanReq, IntPtr kv);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool ScanResponseAddKV(IntPtr scanRes, IntPtr it, bool isMvcc, ulong timestamp);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool ScanFinished(uint shard, IntPtr handle, IntPtr it, IntPtr scanRes, uint errorCode);

    //ScanResponse
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ScanResponseSetSkipped(IntPtr handle, uint skipped);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ScanResponseGetSkipped(IntPtr handle);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ScanResponseGetKV(IntPtr handle, int rowIndex, IntPtr kv);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FreeScanResponse(IntPtr handle);

    //BlobStore
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ExecBlobPrepareWrite(IntPtr waitHandle, ulong source, byte appId, IntPtr cmdIdPtr,
                                           IntPtr pathPtr, uint pathSize, uint size, uint option);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BlobCreateChunk(IntPtr waitHandle, ulong source, byte appId,
                                           IntPtr pathPtr, uint pathSize, uint needSize);
    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BlobWriteChunk(IntPtr waitHandle, ulong source, ulong raftGroupId,
                                           IntPtr pathPtr, uint pathSize, uint option, IntPtr dataPtr);

    internal static class NativeApi
    {
        public static InvokeClrCB InvokeClrCB { get; private set; }

        public static GetPeerConfigData GetPeerConfigData { get; private set; }
        public static ProposeConfChange ProposeConfChange { get; private set; }
        public static PromoteReplFactor PromoteReplFactor { get; private set; }

        public static NewNativeString NewNativeString { get; private set; }
        public static GetStringSize GetStringSize { get; private set; }
        public static GetStringData GetStringData { get; private set; }
        public static FreeNativeString FreeNativeString { get; private set; }

        public static CreateApplication CreateApplication { get; private set; }
        public static GenModelId GenModelId { get; private set; }
        public static MetaGenPartition MetaGenPartition { get; private set; }

        public static NewAlterTable NewAlterTable { get; private set; }
        public static AlterTableAddColumn AlterTableAddColumn { get; private set; }
        public static AlterTableDropColumn AlterTableDropColumn { get; private set; }
        public static AlterTableAddIndex AlterTableAddIndex { get; private set; }
        public static AlterTableDropIndex AlterTableDropIndex { get; private set; }
        public static ExecMetaAlterTable ExecMetaAlterTable { get; private set; }
        public static ExecMetaDropTable ExecMetaDropTable { get; private set; }

        public static BeginTransaction BeginTransaction { get; private set; }
        public static CommitTransaction CommitTransaction { get; private set; }
        public static RollbackTransaction RollbackTransaction { get; private set; }

        public static ExecKVInsert ExecKVInsert { get; private set; }
        public static ExecKVUpdate ExecKVUpdate { get; private set; }
        public static ExecKVDelete ExecKVDelete { get; private set; }
        public static ExecKVAddRef ExecKVAddRef { get; private set; }

        public static ReadIndexByGet ReadIndexByGet { get; private set; }
        public static ReadIndexByScan ReadIndexByScan { get; private set; }
        public static ScanGetReqInfo ScanGetReqInfo { get; private set; }
        public static ScanNextValidIterator ScanNextValidIterator { get; private set; }
        public static ScanResponseAddKV ScanResponseAddKV { get; private set; }
        public static ScanFinished ScanFinished { get; private set; }

        public static ScanResponseSetSkipped ScanResponseSetSkipped { get; private set; }
        public static ScanResponseGetSkipped ScanResponseGetSkipped { get; private set; }
        public static ScanResponseGetKV ScanResponseGetKV { get; private set; }
        public static FreeScanResponse FreeScanResponse { get; private set; }

        public static ExecBlobPrepareWrite ExecBlobPrepareWrite { get; private set; }
        public static BlobCreateChunk BlobCreateChunk { get; private set; }
        public static BlobWriteChunk BlobWriteChunk { get; private set; }

        internal unsafe static void InitDelegates(void** apis)
        {
            InvokeClrCB = Marshal.GetDelegateForFunctionPointer<InvokeClrCB>(new IntPtr(apis[0]));
            GetPeerConfigData = Marshal.GetDelegateForFunctionPointer<GetPeerConfigData>(new IntPtr(apis[37]));
            ProposeConfChange = Marshal.GetDelegateForFunctionPointer<ProposeConfChange>(new IntPtr(apis[38]));
            PromoteReplFactor = Marshal.GetDelegateForFunctionPointer<PromoteReplFactor>(new IntPtr(apis[39]));
            //NativeString
            NewNativeString = Marshal.GetDelegateForFunctionPointer<NewNativeString>(new IntPtr(apis[1]));
            FreeNativeString = Marshal.GetDelegateForFunctionPointer<FreeNativeString>(new IntPtr(apis[2]));
            GetStringSize = Marshal.GetDelegateForFunctionPointer<GetStringSize>(new IntPtr(apis[3]));
            GetStringData = Marshal.GetDelegateForFunctionPointer<GetStringData>(new IntPtr(apis[4]));
            //Meta
            CreateApplication = Marshal.GetDelegateForFunctionPointer<CreateApplication>(new IntPtr(apis[5]));
            GenModelId = Marshal.GetDelegateForFunctionPointer<GenModelId>(new IntPtr(apis[6]));
            MetaGenPartition = Marshal.GetDelegateForFunctionPointer<MetaGenPartition>(new IntPtr(apis[7]));
            //AlterTable
            NewAlterTable = Marshal.GetDelegateForFunctionPointer<NewAlterTable>(new IntPtr(apis[8]));
            AlterTableAddColumn = Marshal.GetDelegateForFunctionPointer<AlterTableAddColumn>(new IntPtr(apis[9]));
            AlterTableDropColumn = Marshal.GetDelegateForFunctionPointer<AlterTableDropColumn>(new IntPtr(apis[10]));
            AlterTableAddIndex = Marshal.GetDelegateForFunctionPointer<AlterTableAddIndex>(new IntPtr(apis[34]));
            AlterTableDropIndex = Marshal.GetDelegateForFunctionPointer<AlterTableDropIndex>(new IntPtr(apis[35]));
            ExecMetaAlterTable = Marshal.GetDelegateForFunctionPointer<ExecMetaAlterTable>(new IntPtr(apis[11]));
            //DropTable
            ExecMetaDropTable = Marshal.GetDelegateForFunctionPointer<ExecMetaDropTable>(new IntPtr(apis[36]));
            //Transaction
            BeginTransaction = Marshal.GetDelegateForFunctionPointer<BeginTransaction>(new IntPtr(apis[12]));
            CommitTransaction = Marshal.GetDelegateForFunctionPointer<CommitTransaction>(new IntPtr(apis[13]));
            RollbackTransaction = Marshal.GetDelegateForFunctionPointer<RollbackTransaction>(new IntPtr(apis[14]));
            //CRUD
            ExecKVInsert = Marshal.GetDelegateForFunctionPointer<ExecKVInsert>(new IntPtr(apis[15]));
            ExecKVUpdate = Marshal.GetDelegateForFunctionPointer<ExecKVUpdate>(new IntPtr(apis[16]));
            ExecKVDelete = Marshal.GetDelegateForFunctionPointer<ExecKVDelete>(new IntPtr(apis[17]));
            ExecKVAddRef = Marshal.GetDelegateForFunctionPointer<ExecKVAddRef>(new IntPtr(apis[33]));

            ReadIndexByGet = Marshal.GetDelegateForFunctionPointer<ReadIndexByGet>(new IntPtr(apis[18]));
            ReadIndexByScan = Marshal.GetDelegateForFunctionPointer<ReadIndexByScan>(new IntPtr(apis[19]));
            ScanGetReqInfo = Marshal.GetDelegateForFunctionPointer<ScanGetReqInfo>(new IntPtr(apis[20]));
            ScanNextValidIterator = Marshal.GetDelegateForFunctionPointer<ScanNextValidIterator>(new IntPtr(apis[21]));
            ScanResponseAddKV = Marshal.GetDelegateForFunctionPointer<ScanResponseAddKV>(new IntPtr(apis[23]));
            ScanFinished = Marshal.GetDelegateForFunctionPointer<ScanFinished>(new IntPtr(apis[25]));

            ScanResponseSetSkipped = Marshal.GetDelegateForFunctionPointer<ScanResponseSetSkipped>(new IntPtr(apis[26]));
            ScanResponseGetSkipped = Marshal.GetDelegateForFunctionPointer<ScanResponseGetSkipped>(new IntPtr(apis[27]));
            ScanResponseGetKV = Marshal.GetDelegateForFunctionPointer<ScanResponseGetKV>(new IntPtr(apis[28]));
            FreeScanResponse = Marshal.GetDelegateForFunctionPointer<FreeScanResponse>(new IntPtr(apis[29]));

            ExecBlobPrepareWrite = Marshal.GetDelegateForFunctionPointer<ExecBlobPrepareWrite>(new IntPtr(apis[30]));
            BlobCreateChunk = Marshal.GetDelegateForFunctionPointer<BlobCreateChunk>(new IntPtr(apis[31]));
            BlobWriteChunk = Marshal.GetDelegateForFunctionPointer<BlobWriteChunk>(new IntPtr(apis[32]));
        }
    }
}
