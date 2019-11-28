using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace appbox.Host
{
    public static class WebHost
    {
        private static IWebHost webHost;
        private static CancellationTokenSource stopCTS = new CancellationTokenSource();
        private static ushort ListenPort;

        public static void StartAsync(ushort port)
        {
            ListenPort = port;
            webHost = CreateWebHostBuilder(null).Build();
            webHost.RunAsync(stopCTS.Token); //使用此方式不会出现"Press Ctrl+C stop application"
        }

        public static void Stop()
        {
            stopCTS.Cancel(); //webHost.StopAsync();
            webHost.WaitForShutdown();
        }

        /// <summary>
        /// 仅用于测试
        /// </summary>
        internal static void Run(string[] args)
        {
            ListenPort = 5000;
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                     .UseKestrel(options =>
                     {
                         options.Listen(System.Net.IPAddress.Any, ListenPort);
                     });
    }
}
