using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using appbox.Caching;

namespace appbox.Server.Channel
{
    internal static class WebSocketManager
    {

        private static readonly Dictionary<ulong, WebSocketClient> clients = new Dictionary<ulong, WebSocketClient>();
        //TODO:use rwlock, 另想办法不需要此字典表

        internal static async Task OnAccept(HttpContext context, WebSocket webSocket)
        {
            //验证是否是已登录用户
            var webSession = context.Session.LoadWebSession();
            Log.Debug($"接受WebSocket连接, {webSocket.GetType()} Session = {webSession}");
            //TODO:没有Session即没有登录先则直接关闭连接

            //加入至列表
            var client = new WebSocketClient(webSocket, webSession);
            lock (clients)
            {
                clients.Add(webSession.SessionID, client);
            }

            //开始接收数据
            ValueWebSocketReceiveResult result;
            BytesSegment frame;
            do
            {
                frame = BytesSegment.Rent();
                try
                {
                    result = await webSocket.ReceiveAsync(frame.Buffer.AsMemory(), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        BytesSegment.ReturnOne(frame);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    BytesSegment.ReturnOne(frame);
                    Log.Warn($"WebSocket receive error: {ex.Message}");
                    break;
                }
                frame.Length = result.Count;

                await client.OnReceiveMessage(frame, result.EndOfMessage); //不需要捕获异常
            } while (true);

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
                clients.Remove(webSession.SessionID);
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
                if (clients.TryGetValue(sessionID, out WebSocketClient client))
                    return client.Session;
                return null;
            }
        }

    }

}
