using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Prometheus;
using appbox.Server;

namespace appbox.Host
{
    /// <summary>
    /// 主进程消息处理器，处理来自应用子进程及调试子进程的消息
    /// </summary>
    public sealed class HostMessageDispatcher : IMessageDispatcher
    {
        /// <summary>
        /// 仅用于调试子进程通道
        /// </summary>
        private IDebugSessionManager _debugSessionManager;

        public HostMessageDispatcher() { }

        public HostMessageDispatcher(IDebugSessionManager debugSessionManager)
        {
            _debugSessionManager = debugSessionManager ?? throw new ArgumentNullException(nameof(debugSessionManager));
        }

        public unsafe void ProcessMessage(IMessageChannel channel, MessageChunk* first)
        {
            switch ((MessageType)first->Type)
            {
                case MessageType.InvokeResponse:
                    ProcessInvokeResponse(channel, first); break;
                case MessageType.MetricRequire:
                    ProcessMetricRequire(channel, first); break;
                case MessageType.KVGetRequire:
                    ProcessReadIndexByGet(channel, first); break;
                case MessageType.KVScanRequire:
                    ProcessReadIndexByScan(channel, first); break;
                case MessageType.BeginTranRequire:
                    ProcessBeginTran(channel, first); break;
                case MessageType.CommitTranRequire:
                    ProcessCommitTran(channel, first); break;
                case MessageType.RollbackTranRequire:
                    ProcessRollbackTran(channel, first); break;
                case MessageType.GenPartitionRequire:
                    ProcessGenPartition(channel, first); break;
                case MessageType.KVInsertRequire:
                    ProcessKVInsert(channel, first); break;
                case MessageType.KVUpdateRequire:
                    ProcessKVUpdate(channel, first); break;
                case MessageType.KVDeleteRequire:
                    ProcessKVDelete(channel, first); break;
                case MessageType.KVAddRefRequire:
                    ProcessKVAddRef(channel, first); break;
                default:
                    channel.ReturnMessageChunks(first);
                    Log.Warn($"Unknow MessageType: {first->Type}");
                    break;
            }
        }

        private unsafe void ProcessInvokeResponse(IMessageChannel channel, MessageChunk* first)
        {
            //注意:先读取消息头信息确认消息回复的目标
            var reqSource = InvokeResponse.GetSourceFromMessageChunk(first);
            switch (reqSource)
            {
                case InvokeSource.Client:
                    {
                        IntPtr waitHandle = InvokeResponse.GetWaitHandleFromMessageChunk(first);
                        GCHandle tcsHandle = GCHandle.FromIntPtr(waitHandle);
                        var tcs = (TaskCompletionSource<object>)tcsHandle.Target;
                        tcs.SetResult(new IntPtr(first)); //TODO:考虑包装Channel及Chunk
                        tcsHandle.Free();
                    }
                    break;
                case InvokeSource.Host:
                    {
                        var response = channel.Deserialize<InvokeResponse>(first); //TODO:处理反序列化异常
                        GCHandle tcsHandle = GCHandle.FromIntPtr(response.WaitHandle);
                        var tcs = (TaskCompletionSource<object>)tcsHandle.Target;
                        if (response.Error == InvokeResponseError.None)
                            tcs.SetResult(response.Result);
                        else
                            tcs.SetException(new Exception(response.Error.ToString()));
                        tcsHandle.Free();
                    }
                    break;
                case InvokeSource.Debugger:
                    {
                        var response = channel.Deserialize<InvokeResponse>(first); //TODO:处理反序列化异常
                        _debugSessionManager.GotInvokeResponse(channel.RemoteRuntimeId, response); //注意暂直接在当前线程处理
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private unsafe void ProcessMetricRequire(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<MetricRequire>(first);
            ServerMetrics.InvokeDuration.WithLabels(req.Service).Observe(req.Value);
        }

        private unsafe void ProcessReadIndexByGet(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVGetRequire>(first);
            NativeApi.ReadIndexByGet(req.WaitHandle, channel.RemoteRuntimeId, req.RaftGroupId, req.KeyPtr, req.KeySize, req.DataCF);
            req.FreeData();
        }

        private unsafe void ProcessReadIndexByScan(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVScanRequire>(first);
            NativeApi.ReadIndexByScan(req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&req));
            req.FreeKeysData();
        }

        private unsafe void ProcessBeginTran(IMessageChannel channel, MessageChunk* first)
        {
            var req = new BeginTranRequire();
            req.FastReadFrom(first);
            channel.ReturnMessageChunks(first);
            NativeApi.BeginTransaction(req.ReadCommitted, req.WaitHandle, channel.RemoteRuntimeId);
        }

        private unsafe void ProcessCommitTran(IMessageChannel channel, MessageChunk* first)
        {
            var req = new CommitTranRequire();
            req.FastReadFrom(first);
            channel.ReturnMessageChunks(first);
            NativeApi.CommitTransaction(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId);
        }

        private unsafe void ProcessRollbackTran(IMessageChannel channel, MessageChunk* first)
        {
            var req = new RollbackTranRequire();
            req.FastReadFrom(first);
            channel.ReturnMessageChunks(first);
            NativeApi.RollbackTransaction(req.TxnPtr, req.IsAbort);
        }

        private unsafe void ProcessGenPartition(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<GenPartitionRequire>(first);
            var info = req.PartitionInfo;
            NativeApi.MetaGenPartition(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&info));
            req.FreeData();
        }

        private unsafe void ProcessKVInsert(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVInsertRequire>(first);
            NativeApi.ExecKVInsert(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&req));
            req.FreeData();
        }

        private unsafe void ProcessKVUpdate(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVUpdateRequire>(first);
            NativeApi.ExecKVUpdate(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&req));
            req.FreeData();
        }

        private unsafe void ProcessKVDelete(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVDeleteRequire>(first);
            NativeApi.ExecKVDelete(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&req));
            req.FreeData();
        }

        private unsafe void ProcessKVAddRef(IMessageChannel channel, MessageChunk* first)
        {
            var req = channel.Deserialize<KVAddRefRequire>(first);
            NativeApi.ExecKVAddRef(req.TxnPtr, req.WaitHandle, channel.RemoteRuntimeId, new IntPtr(&req));
            req.FreeData();
        }
    }
}
