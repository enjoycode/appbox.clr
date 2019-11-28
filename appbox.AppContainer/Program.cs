using System;
using System.Runtime.InteropServices;
using System.Threading;
using appbox.Runtime;
using appbox.Server;

namespace appbox.AppContainer
{
    class Program
    {
        /// <summary>
        /// 1. 一个参数（PeerId）表明运行时子进程
        /// 2. 两个参数（PeerId + DebugSessionId）表明服务调试子进程
        /// </summary>
        static void Main(string[] args)
        {
            ushort peerId = ushort.Parse(args[0]);
            Log.Debug($"AppContainer start. PeerId={peerId}");

            //初始化运行时
            var runtimeCtx = new AppRuntimeContext();
            RuntimeContext.Init(runtimeCtx, peerId);

            if (args.Length == 1)
            {
                //建立通道
                runtimeCtx.Channel = new SharedMemoryChannel("AppChannel", new AppMessageDispatcher());
                //初始化存储
                Store.StoreApi.Init(new Store.AppStoreApi(runtimeCtx.Channel));
                //开始接收并处理消息
                runtimeCtx.Channel.StartReceiveOnCurrentThread();
            }
            else //----调试子进程----
            {
                if (ulong.TryParse(args[1], out ulong debugSessionId))
                {
                    //注入调试用的服务模型至ServiceInstanceContainer内，以防止从数据库加载
                    runtimeCtx.services.InjectDebugService(debugSessionId);
                    //同上建立通道并初始化存储
                    runtimeCtx.Channel = new SharedMemoryChannel(debugSessionId.ToString(), new AppMessageDispatcher());
                    Store.StoreApi.Init(new Store.AppStoreApi(runtimeCtx.Channel));
                    //发送已准备好消息给Host进程

                    //开始接收并处理消息
                    runtimeCtx.Channel.StartReceiveOnCurrentThread();
                }
                else
                {
                    Log.Warn("Cannot parse debug session id.");
                }
            }

            Log.Warn("AppContainer exit.");
        }
    }

}
