using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace appbox.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

#if !FUTURE
            //加载默认SqlStore
            var asm = Assembly.LoadFile(Path.Combine(Runtime.RuntimeContext.Current.AppPath,
                Server.Consts.LibPath, $"{configuration["DefaultSqlStore:Assembly"]}.dll"));
            var type = asm.GetType(configuration["DefaultSqlStore:Type"]);
            var sqlStore = (Store.SqlStore)Activator.CreateInstance(type,
                configuration["DefaultSqlStore:ConnectionString"]);
            Store.SqlStore.SetDefaultSqlStore(sqlStore);
            //暂在这里尝试初始化默认存储
            Store.ModelStore.TryInitStoreAsync().Wait();
#endif
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddResponseCompression();
            services.AddControllers();  //services.AddMvc();

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache(); //todo:根据是否分布式部署选择不同的实现

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Runtime.RuntimeContext.Current.AppPath, "wwwroot"))
            });

            app.UseRouting(); //必须在UseStaticFiles之后
            //app.UseCors();

            app.UseWebSockets(new WebSocketOptions()
            {
                ReceiveBufferSize = 8192
            });

            app.UseSession();

            //使用自定义中间件(Handler)
            app.UseInvokeMiddleware();
            //TODO:改造为中间件
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/wsapi") //通过WebSocket通道进行Api调用
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Server.Channel.WebSocketManager.OnAccept(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                //else if (context.Request.Path == "/ssh")
                //{
                //    if (context.WebSockets.IsWebSocketRequest)
                //    {
                //        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                //        await SSHManager.OnAccept(context, webSocket);
                //    }
                //    else
                //    {
                //        context.Response.StatusCode = 400;
                //    }
                //}
                else
                {
                    await next();
                }
            });
            //app.UseMetricsMiddleware();

            app.Map("/dev", con =>
            {
                con.Run(async context =>
                {
                    context.Response.Redirect("/dev/index.html");
                    await Task.FromResult(0);
                });
            });
            app.Map("/ops", con =>
            {
                con.Run(async context =>
                {
                    context.Response.Redirect("/app/index.html#/ops");
                    await Task.FromResult(0);
                });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");
            });

			try
			{
				// obd.ObdDataReceiveService.GetVINInfo("LB115263589752657");
				// 启动Gateway
				StartGateway(Configuration["DefaultSqlStore:ConnectionString"]);
				Log.Info("网关已启动");
			}
			catch (System.Exception ex)
			{
				Log.Error(ex.Message);
			}
        }

		void StartGateway(string connectionString)
		{
			var settings = new OBDGateway.ServerSettings
            {
                TcpPort = 61100,
                DataStore = new OBDGateway.PGSqlStore(connectionString),
                // GetVINInfo = obd.ObdDataReceiveService.GetVINInfo,
                VehicleOnline = obd.ObdDataReceiveService.VehicleOnline,
                VehicleOffline = obd.ObdDataReceiveService.VehicleOffline,
				Alarm = obd.ObdDataReceiveService.VehicleAlarm
            };

            OBDGateway.Server.Run(settings);

			// obd.ObdDataReceiveService.PublishEventTest();
		}
    }
}
