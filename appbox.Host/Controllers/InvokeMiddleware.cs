using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using appbox.Caching;
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
            //暂简单处理，读请求数据至缓存块，然后走与WebSocket相同的流程
            //待实现Utf8JsonReaderStream后再修改
            int bytesRead = 0;
            int totalBytes = (int)context.Request.ContentLength.Value;
            BytesSegment frame = null;
            while (bytesRead < totalBytes) //TODO:读错误归还缓存块
            {
                var temp = BytesSegment.Rent();
                int len = await context.Request.Body.ReadAsync(temp.Buffer.AsMemory());
                temp.Length = len;
                bytesRead += len;
                if (frame != null)
                    frame.Append(temp);
                frame = temp;
            }

            //1. 解析请求头
            int msgId = 0;
            string service = null; //TODO:优化不创建string
            int offset = 0; //偏移量至参数数组开始，不包含[Token
            AnyValue res = AnyValue.Empty;
            try
            {
                offset = InvokeHelper.ReadRequireHead(frame.First, ref msgId, ref service);
                if (offset == -1) //没有参数
                    BytesSegment.ReturnOne(frame);
            }
            catch (Exception ex)
            {
                res = AnyValue.From(ex);
                BytesSegment.ReturnAll(frame); //读消息头异常归还缓存块
                Log.Warn($"收到无效的Api调用请求: {ex.Message}");
            }

            //2. 设置当前会话并调用服务
            if (res.ObjectValue == null)
            {
                var webSession = context.Session.LoadWebSession();
                RuntimeContext.Current.CurrentSession = webSession;

                try
                {
                    var hostCtx = (HostRuntimeContext)RuntimeContext.Current;
                    res = await hostCtx.InvokeByClient(service, 0, InvokeArgs.From(frame, offset));
                }
                catch (Exception ex)
                {
                    Log.Warn($"调用服务异常: {ex.Message}\n{ex.StackTrace}");
                    res = AnyValue.From(ex);
                }
            }

            //3. 返回结果，注意：FileResult特殊类型统一由BlobController处理
            context.Response.ContentType = "application/json";
            if (res.Type == AnyValueType.Object && res.ObjectValue is BytesSegment)
            {
                var cur = (ReadOnlySequenceSegment<byte>)res.ObjectValue;
                try
                {
                    while (cur != null)
                    {
                        await context.Response.Body.WriteAsync(cur.Memory);
                        cur = cur.Next;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Send InvokeResponse to ajax error: {ex.Message}");
                }
                finally
                {
                    BytesSegment.ReturnAll((BytesSegment)res.ObjectValue); //注意归还缓存块
                }
            }
            else
            {
                res.SerializeAsInvokeResponse(context.Response.Body, 0);//TODO:序列化错误处理
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
