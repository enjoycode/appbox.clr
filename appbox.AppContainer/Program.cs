using System;
using appbox.Runtime;
using appbox.Server;
#if !FUTURE
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
#endif

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
#if FUTURE
                Store.StoreApi.Init(new Store.AppStoreApi(runtimeCtx.Channel));
#else
                SetDefaultSqlStore();
#endif
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
#if FUTURE
                    Store.StoreApi.Init(new Store.AppStoreApi(runtimeCtx.Channel));
#else
                    SetDefaultSqlStore();
#endif
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

#if !FUTURE
        private static void SetDefaultSqlStore()
        {
            var settingFile = Path.Combine(RuntimeContext.Current.AppPath, "appsettings.json");
            var settings = JObject.Parse(File.ReadAllText(settingFile));
            //根据配置加载默认的DataStore实例
            var storeSetting = settings["DefaultSqlStore"].ToObject<DefaultStoreSetting>();
            var asm = Assembly.LoadFile(Path.Combine(RuntimeContext.Current.AppPath, Server.Consts.LibPath, storeSetting.Assembly + ".dll"));
            var sqlStore = (Store.SqlStore)Activator.CreateInstance(asm.GetType(storeSetting.Type), storeSetting.ConnectionString);
            Store.SqlStore.SetDefaultSqlStore(sqlStore);
        }
#endif

    }

#if !FUTURE
    struct DefaultStoreSetting
    {
        public string Assembly { get; set; }
        public string Type { get; set; }
        public string ConnectionString { get; set; }
    }
#endif

}
