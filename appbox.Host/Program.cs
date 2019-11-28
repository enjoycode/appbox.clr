using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using appbox.Runtime;
using appbox.Server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace appbox.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello Future!");

            //初始化运行时
            RuntimeContext.Init(new HostRuntimeContext(), 0x1041);
            Server.Runtime.SysServiceContainer.Init();

            //启动应用子进程通道
            ChildProcess.StartAppContainer();

            //启动WebHost
            WebHost.Run(args);
        }
    }
}
