using System;

namespace appbox.Server
{
    public enum MessageType : byte
    {
        RawData,
        InvalidModelsCache,

        //LoginRequire = 5,
        //LoginResponse,

        InvokeRequire = 10,
        InvokeResponse,

#if FUTURE
        NativeMessage, //特指StoreApi回复
        KVGetRequire,
        KVScanRequire,
        BeginTranRequire,
        CommitTranRequire,
        RollbackTranRequire,
        GenPartitionRequire,
        KVInsertRequire,
        KVUpdateRequire,
        KVAddRefRequire,
        KVDeleteRequire,
#endif

        MetricRequire,
        //注意255保留，用于退出读取Loop的特殊消息
    }
}
