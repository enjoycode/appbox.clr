using appbox.Runtime;
using appbox.Server;

namespace appbox.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if Windows
            //临时方案修复调试器的json编码问题
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
#endif

            //初始化运行时
            RuntimeContext.Init(new HostRuntimeContext(), 0x1041); //TODO:fix peerid
            Server.Runtime.SysServiceContainer.Init();

            //启动应用子进程
            ChildProcess.StartAppContainer();

            //设置调试服务创建消息通道的委托
            Design.DebugSessionManager.DebugMessageDispatcherMaker =
                (debugSessionManager) => new HostMessageDispatcher(debugSessionManager);

            //启动WebHost
            WebHost.Run(args);
        }
    }
}
