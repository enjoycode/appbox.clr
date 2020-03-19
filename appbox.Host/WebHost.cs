using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace appbox.Host
{
    public static class WebHost
    {
        private static IHost webHost;
        private static readonly CancellationTokenSource stopCTS = new CancellationTokenSource();
        private static ushort ListenPort;

        public static void StartAsync(ushort port)
        {
            ListenPort = port;
            webHost = CreateHostBuilder(null).Build();
            webHost.RunAsync(stopCTS.Token); //使用此方式不会出现"Press Ctrl+C stop application"
        }

        public static void Stop()
        {
            stopCTS.Cancel(); //webHost.StopAsync();
            webHost.WaitForShutdown();
        }

        internal static void Run(string[] args)
        {
            ListenPort = 5000;
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(System.Net.IPAddress.Any, ListenPort);
                    })
                    .UseStartup<Startup>();
                });

    }
}
