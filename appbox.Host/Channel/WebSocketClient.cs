using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using appbox.Data;
using appbox.Caching;
using appbox.Runtime;
using System.Threading.Tasks;
using System.Text.Json;

namespace appbox.Server.Channel
{
    sealed class WebSocketClient
    {

        readonly WebSocket socket;
        internal WebSession Session { get; }
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        BytesSegment pending;

        internal WebSocketClient(WebSocket socket, WebSession session)
        {
            if (session != null)
                session.Owner = this;

            this.socket = socket;
            Session = session;
        }

        internal async ValueTask OnReceiveMessage(BytesSegment frame, bool isEnd)
        {
            if (!isEnd)
            {
                if (pending != null)
                    pending.Append(frame);
                pending = frame;
            }
            else
            {
                //检查有没有前面的消息帧
                if (pending != null)
                {
                    pending.Append(frame);
                    pending = null;
                }

                //开始读取消息
                int msgId = 0;
                string service = null; //TODO:优化不创建string
                int offset = 0; //偏移量至参数数组开始，不包含[Token
                Exception requireError = null;
                try
                {
                    offset = InvokeHelper.ReadRequireHead(frame.First, ref msgId, ref service);
                    //offset = -1暂不要在这里归还frame，后面invoke统一归还
                }
                catch (Exception ex)
                {
                    requireError = ex;
                    BytesSegment.ReturnAll(frame); //读消息头异常释放WebSocketFrame
                }

                if (requireError != null)
                {
                    Log.Warn(string.Format("收到无效的Api调用请求: {0}", requireError.Message));
                    await SendInvokeResponse(msgId, AnyValue.From(requireError));
                    return;
                }

                _ = ProcessInvokeRequire(msgId, service, frame, offset); //no need await
            }
        }

        private async ValueTask ProcessInvokeRequire(int msgId, string service, BytesSegment frame, int offset)
        {
            //设置当前会话,TODO:考虑在后面设置
            RuntimeContext.Current.CurrentSession = Session;
            //注意：不要使用Task.ContinueWith, 异常会传播至上级
            try
            {
                var hostCtx = ((HostRuntimeContext)RuntimeContext.Current);
                var res = await hostCtx.InvokeByClient(service, msgId, frame, offset);
                await SendInvokeResponse(msgId, res);
            }
            catch (Exception ex)
            {
                await SendInvokeResponse(msgId, AnyValue.From(ex));
                Log.Warn($"Invoke error: {ExceptionHelper.GetExceptionDetailInfo(ex)}");
            }
        }

        private async Task SendInvokeResponse(int msgId, AnyValue res)
        {
            //if (res is IntPtr ptr) //返回结果服务域已经序列化
            //{
            //    if (ptr != IntPtr.Zero)
            //    {
            //        await sendLock.WaitAsync();
            //        try
            //        {
            //            await InvokeHelper.SendInvokeResponse(socket, ptr);
            //        }
            //        finally
            //        {
            //            sendLock.Release();
            //        }
            //    }
            //    else
            //        Log.Warn("收到服务域调用结果为空");
            //}
            //else
            //{
            byte[] data = null;
            bool serializeError = false;
            using (var ms = new MemoryStream()) //todo: 暂用MemoryStream，待用WebSocketFrameWriteStream替代
            {
                using (var sw = new StreamWriter(ms))
                {
                    serializeError = InvokeHelper.WriteInvokeResponse(sw, msgId, res.BoxedValue);
                }
                data = ms.ToArray();
            }

            if (!serializeError && socket.State == WebSocketState.Open)
            {
                //注意：如果用WebSocketFrameWriteStream实现，待实现发送队列
                await sendLock.WaitAsync();
                try
                {
                    await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                finally
                {
                    sendLock.Release();
                }
            }
            //}
        }

        internal void SendEvent(int source, string body)
        {
            string msg = string.Format("{{\"ES\":{0},\"BD\":{1}}}", source, body);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);

            if (socket.State == WebSocketState.Open)
            {
                Task.Run(async () =>
                {
                    await sendLock.WaitAsync();
                    try
                    {
                        //注意：如果用WebSocketFrameWriteStream实现，待实现发送队列
                        await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    finally
                    {
                        sendLock.Release();
                    }
                });
            }
        }

    }
}