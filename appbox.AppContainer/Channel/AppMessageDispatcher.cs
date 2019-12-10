using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Server;

namespace appbox.AppContainer
{
    sealed class AppMessageDispatcher : IMessageDispatcher
    {
        public unsafe void ProcessMessage(IMessageChannel channel, MessageChunk* first)
        {
            switch ((MessageType)first->Type)
            {
                case MessageType.InvalidModelsCache:
                    ProcessInvalidModelsCache(channel, first); break;
                case MessageType.InvokeRequire:
                    ProcessInvokeRequire(channel, new IntPtr(first)); break;
                case MessageType.NativeMessage:
                    ProcessStoreCB(channel, first); break;
                default:
                    channel.ReturnMessageChunks(first);
                    Log.Warn($"Unknow MessageType: {first->Type}");
                    break;
            }
        }

        private unsafe void ProcessInvalidModelsCache(IMessageChannel channel, MessageChunk* first)
        {
            var msg = channel.Deserialize<InvalidModelsCache>(first);
            Runtime.RuntimeContext.Current.InvalidModelsCache(msg.Services, msg.Models, false);
        }

        private void ProcessInvokeRequire(IMessageChannel channel, IntPtr first)
        {
            InvokeRequire require;
            try
            {
                unsafe
                {
                    require = channel.Deserialize<InvokeRequire>((MessageChunk*)first.ToPointer());
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"反序列化InvokeRequire错误: {ex.Message}");
                //TODO:直接发送反序列化错误回复，需要修改Channel.Deserialize入参引用InvokeRequire
                return;
            }

            try
            {
                var enqueueOk = ThreadPool.QueueUserWorkItem(async (req) =>
                {
                    InvokeResponse response;

                    //TODO:调用服务并根据请求来源及协议进行相应的序列化后发送回Host进程
                    //Log.Debug($"收到InvokeRequire: {req.Service}");
                    //var res = await TestReadPerf();
                    //var res = await TestInsertPerf();

                    //设置会话信息后调用服务
                    Runtime.RuntimeContext.Current.CurrentSession = req.Session;
                    try
                    {
                        var res = await Runtime.RuntimeContext.Current.InvokeAsync(req.Service, req.Args);
                        response = new InvokeResponse(req.Source, req.ContentType, req.WaitHandle, req.SourceMsgId, res);
                    }
                    catch (Exception ex)
                    {
                        response = new InvokeResponse(req.Source, req.ContentType, req.WaitHandle, req.SourceMsgId,
                                                      InvokeResponseError.ServiceInnerError, ex.Message);
                        Log.Warn($"Service internal error: {ExceptionHelper.GetExceptionDetailInfo(ex)}");
                    }
                    finally
                    {
                        req.Args.ReturnBuffer(); //注意归还由主进程封送过来的参数缓存块
                    }

                    //最后发送回复，注意：序列化失败会标记消息Flag为序列化错误
                    try
                    {
                        channel.SendMessage(ref response);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"[AppContainer]发送回复错误: {ex.Message}");
                    }
                }, require, preferLocal: false);

                if (!enqueueOk)
                    Log.Debug("无法加入线程池处理");
            }
            catch (NotSupportedException)
            {
                unsafe { channel.ReturnMessageChunks((MessageChunk*)first.ToPointer()); }
                //TODO:直接发送异常回复
                Log.Warn("[AppContainer]未能加入到线程池处理InvokeRequire");
            }
        }

        private unsafe void ProcessStoreCB(IMessageChannel channel, MessageChunk* first)
        {
            var msg = channel.Deserialize<NativeMessage>(first);
            //Log.Debug($"收到存储回调信息: Type={(StoreCBType)msg.Shard} WaitHandle={msg.Handle}");
            GCHandle tsHandle = GCHandle.FromIntPtr(msg.Handle);
            //注意：必须启用线程池，否则ValueTask.Continue时如果存在异步转同步调用(ValueTask.Result)会阻塞消息循环
            var ts = (PooledTaskSource<NativeMessage>)tsHandle.Target;
            bool ok = ts.NotifyCompletionOnThreadPool(msg);
            if (!ok)
                Log.Warn("无法加入线程池");
        }

        #region ====Test Methods====
        private static int nameCount;
        private async ValueTask InsertEmploeeAsync()
        {
            var index = Interlocked.Increment(ref nameCount);
            string name = $"AAAAA{index}";
            var txn = await Store.Transaction.BeginAsync();
            try
            {
                var model = await Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_EMPLOEE_MODEL_ID);
                var emp1 = new Data.Entity(model);
                emp1.SetString(Consts.EMPLOEE_NAME_ID, name);
                emp1.SetString(Consts.EMPLOEE_ACCOUNT_ID, name);
                await Store.EntityStore.InsertEntityAsync(emp1, txn);
                await txn.CommitAsync();
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                txn.Rollback();
            }
        }

        private async ValueTask<string> TestInsertPerf()
        {
            return await SimplePerfTest.Run(64, 1000, async (i, j) =>
            {
                await InsertEmploeeAsync();
            });
        }

        private async ValueTask<string> TestReadPerf()
        {
            return await SimplePerfTest.Run(64, 2000, async (i, j) =>
            {
                await Store.ModelStore.LoadApplicationAsync(Consts.SYS_APP_ID);
            });
        }
        #endregion
    }
}
