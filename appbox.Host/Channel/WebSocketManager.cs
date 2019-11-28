using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace appbox.Server.Channel
{
    internal static class WebSocketManager
    {

        static readonly Dictionary<WebSocket, WebSocketClient> clients = new Dictionary<WebSocket, WebSocketClient>();
        //TODO:use rwlock

        internal static async Task OnAccept(HttpContext context, WebSocket webSocket)
        {
            //验证是否是已登录用户
            var webSession = context.Session.LoadWebSession();
            Log.Debug($"接受WebSocket连接, {webSocket.GetType()} Session = {webSession}");

            //加入至列表
            lock (clients)
            {
                clients.Add(webSocket, new WebSocketClient(webSocket, webSession));
            }

            //开始接收数据
            WebSocketReceiveResult result = null;
            try
            {
                do
                {
                    var frame = WebSocketFrame.Pop();
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(frame.Buffer), CancellationToken.None);
                    if (result.CloseStatus.HasValue)
                    {
                        WebSocketFrame.Push(frame); //释放缓存
                        break;
                    }
                    frame.Length = result.Count;

                    //找到WebSocketClient后组装消息
                    WebSocketClient client = null;
                    lock (clients)
                    {
                        clients.TryGetValue(webSocket, out client);
                    }
                    if (client == null)
                    {
                        WebSocketFrame.Push(frame); //释放缓存
                        break;
                    }

                    await client.OnReceiveMessage(frame, result.EndOfMessage);
                } while (true);
            }
            catch (Exception ex)
            {
                Log.Warn($"接收数据错误：{ExceptionHelper.GetExceptionDetailInfo(ex)}");
            }

            try
            {
                // await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Debug($"关闭WebSocket通道失败:{ex.Message}，忽略继续");
            }

            //移除清理
            if (webSession != null)
                webSession.Dispose();

            var leftCount = 0;
            lock (clients)
            {
                clients.Remove(webSocket);
                leftCount = clients.Count;
            }
            Log.Debug(string.Format("WebSocket关闭, 还余: {0}", leftCount));
        }

        /// <summary>
        /// 根据会话标识获取设计时的会话，主要用于Ajax通道
        /// </summary>
        /// <returns>找不到返回null</returns>
        internal static WebSession GetSessionByID(ulong sessionID)
        {
            lock (clients)
            {
                foreach (var item in clients.Values)
                {
                    if (item.Session.SessionID == sessionID)
                        return item.Session;
                }
            }
            return null;
        }

    }

}
