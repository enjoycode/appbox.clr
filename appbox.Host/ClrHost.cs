using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using appbox.Runtime;
using appbox.Server;

namespace appbox
{
    public static class ClrHost
    {
        private static readonly List<NativeMessageHandler> Handlers = new List<NativeMessageHandler>
        {
            m => throw new NotSupportedException("StopHost"),
            InitStore,
            StoreCB,
            Store.HostStoreApi.ScanByClr,
            TestInvokeClr
        };

        /// <summary>
        /// 处理Native->Clr消息
        /// </summary>
        public static unsafe void HandleMessage(IntPtr msgPtr)
        {
            //TODO: Debug时判断类型是否超出范围
            NativeMessage* msg = (NativeMessage*)msgPtr;
            Handlers[(int)(msg->Type)](msgPtr);
        }

        internal static unsafe void InitStore(IntPtr msgPtr)
        {
            NativeMessage* msg = (NativeMessage*)msgPtr;
            uint shard = msg->Shard;
            IntPtr promise = msg->Handle;
            Store.StoreInitiator.InitAsync().ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        var ex = t.Exception.Flatten().InnerException;
                        Console.WriteLine($"Init store failed: {ex.Message}\n{ex.StackTrace}");
                        //TODO:回调处理
                        NativeApi.InvokeClrCB(shard, promise);
                    }
                    else
                    {
                        NativeApi.InvokeClrCB(shard, promise);
                    }
                });
        }

        /// <summary>
        /// 处理Native存储引擎的请求回复
        /// </summary>
        internal static unsafe void StoreCB(IntPtr msgPtr)
        {
            NativeMessage* msg = (NativeMessage*)msgPtr;
            if (msg->Source == 0)
            {
                GCHandle tsHandle = GCHandle.FromIntPtr(msg->Handle);
                var ts = (PooledTaskSource<NativeMessage>)tsHandle.Target;
                //注意：必须启用线程池，否则ValueTask.Continue时如果存在异步转同步调用(ValueTask.Result)会阻塞Native->Clr消息循环
                bool ok = ts.SetResultOnOtherThread(*msg);
                if (!ok)
                {
                    Log.Warn("无法加入线程池");
                    msg->FreeData(); //注意:无法排队处理必须释放存储引擎分配的内存
                }
            }
            else if (msg->Source == 1)
            {
                //TODO:暂直接阻塞发送
                Host.ChildProcess.AppContainer.Channel.SendMessage(ref *msg);
                msg->FreeData(); //注意:发送完必须释放存储引擎分配的内存
            }
            else
            {
                Design.DebugSessionManager.Instance.ForwardStoreMessage(ref *msg);
                msg->FreeData(); //注意:发送完必须释放存储引擎分配的内存
            }
        }

        public unsafe static void StartHost(ushort peerId, ushort port, void** apis)
        {
            //ThreadPool.SetMaxThreads(100, 1000);
            //ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIOThreads);
            //ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIOThreads);
            //Log.Debug($"{minWorkerThreads}-{maxWorkerThreads}  {minIOThreads}-{maxIOThreads}");

            //TODO:捕获异常并返回给Native，终止启动流程
            //初始化Native apis
            NativeApi.InitDelegates(apis);
            Store.StoreApi.Init(new Store.HostStoreApi());

            //初始化运行时
            RuntimeContext.Init(new HostRuntimeContext(), peerId);
            Server.Runtime.SysServiceContainer.Init();

            //启动应用子进程
            Host.ChildProcess.StartAppContainer();

            //设置调试服务创建消息通道的委托
            Design.DebugSessionManager.DebugMessageDispatcherMaker =
                (debugSessionManager) => new Host.HostMessageDispatcher(debugSessionManager);

            //启动WebHost
            Host.WebHost.StartAsync(port);
        }

        public static void StopHost()
        {
            Host.WebHost.Stop();
        }

        /// <summary>
        /// 由ClrHost专有线程调用
        /// </summary>
        internal static unsafe void TestInvokeClr(IntPtr msgPtr)
        {
            //TODO:测试线程上下文有没有传播
            NativeMessage* msg = (NativeMessage*)msgPtr;
            //Console.WriteLine("TestInvokeClr: {0}.{1}.{2}", msg->Shard, msg->Handle, msg->Data1);
            //通知Native Promise
            var msgCopy = *msg;
            ThreadPool.QueueUserWorkItem(m => NativeApi.InvokeClrCB(m.Shard, m.Handle), msgCopy, false);
            //NativeApi.InvokeClrCB(msg->Shard, msg->Handle);
        }
    }

}
