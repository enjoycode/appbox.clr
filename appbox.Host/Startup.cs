using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace appbox.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddResponseCompression();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache(); //todo:根据是否分布式部署选择不同的实现

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Runtime.RuntimeContext.Current.AppPath, "wwwroot"))
            });

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
            //app.UseHttpsRedirection();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });
        }
    }
}
