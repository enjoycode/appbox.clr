using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Runtime;
using appbox.Server;
using appbox.Server.Channel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace appbox.Host
{
    //参考:https://docs.microsoft.com/en-us/aspnet/core/migration/http-modules

    public sealed class InvokeMiddleware
    {
        // Must have constructor with this signature, otherwise exception at run time
        public InvokeMiddleware(RequestDelegate next)
        {
            // This is an HTTP Handler, so no need to store next
        }

        public async Task Invoke(HttpContext context)
        {
            //1. 解析请求Json
            int msgId = 0;
            string service = null;
            InvokeArgs args = new InvokeArgs();
            object res = null;

            try
            {
                using (var sr = new StreamReader(context.Request.Body))
                {
                    InvokeHelper.ReadInvokeRequire(sr, ref msgId, ref service, ref args);
                }
            }
            catch (Exception ex)
            {
                res = ex;
            }

            //2. 设置当前会话并调用服务
            if (res == null)
            {
                var webSession = context.Session.LoadWebSession();
                RuntimeContext.Current.CurrentSession = webSession;

                try
                {
                    res = await ((HostRuntimeContext)RuntimeContext.Current).InvokeByWebClientAsync(service, args, 0);
                }
                catch (Exception ex)
                {
                    Log.Warn($"调用服务异常: {ex.Message}");
                    res = ex;
                }
            }

            //3. 返回结果，注意：FileResult特殊类型统一由BlobController处理
            context.Response.ContentType = "application/json";
            if (res is IntPtr ptr)
            {
                if (ptr != IntPtr.Zero)
                    InvokeHelper.SendInvokeResponse(context.Response.Body, ptr);
                else
                    Log.Warn("收到服务域调用结果为空");
            }
            else
            {
                var exception = res as Exception;
                using (var sw = new StreamWriter(context.Response.Body))
                {
                    InvokeHelper.WriteInvokeResponse(sw, 0, res); //TODO:序列化错误处理
                }
            }
        }
    }

    public static class InvokeMiddlewareExtensions
    {
        public static IApplicationBuilder UseInvokeMiddleware(this IApplicationBuilder builder)
        {
            return builder.Map("/api/invoke", b => b.UseMiddleware<InvokeMiddleware>());
        }

    }
}
