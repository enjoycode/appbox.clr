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
			if(webSession == null)
				return;
			//加入至列表
			var client = new WebSocketClient(webSocket, webSession);
			if (clients.ContainsKey(webSession.SessionID))
			{
				await CloseAndRemoveClientAsync(clients[webSession.SessionID], "当前账号已在其它设备登录");
			}
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

			await CloseAndRemoveClientAsync(client);
			// try
			// {
			//     // await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
			//     await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
			// }
			// catch (Exception ex)
			// {
			//     Log.Debug($"关闭WebSocket通道失败:{ex.Message}，忽略继续");
			// }

			// //移除清理
			// if (webSession != null)
			//     webSession.Dispose();

			// var leftCount = 0;
			// lock (clients)
			// {
			//     clients.Remove(webSession.SessionID);
			//     leftCount = clients.Count;
			// }
			// Log.Debug(string.Format("WebSocket关闭, 还余: {0}", leftCount));
		}

		private static async Task CloseAndRemoveClientAsync(WebSocketClient socketClient, string status = "")
		{
			try
            {
                // await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                await socketClient.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, status, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Debug($"关闭WebSocket通道失败:{ex.Message}，忽略继续");
            }

            //移除清理
            if (socketClient.Session != null)
                socketClient.Session.Dispose();

            var leftCount = 0;
            lock (clients)
            {
                clients.Remove(socketClient.Session.SessionID);
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
		
		// 向所有已登录的用户广播事件通知
		internal static void PublishEvent(object eventData)
		{
			Task.Run(() =>
			{
				try
				{
					foreach (var client in clients.Values)
					{
						string body = null;
						if(eventData != null)
							body = System.Text.Json.JsonSerializer.Serialize(eventData);
						client.SendEvent(3, body);
					}
				}
				catch (System.Exception ex)
				{
					Log.Error("发送广播事件出错;" + ex.Message, nameof(WebSocketManager), nameof(PublishEvent));
				}
			});
		}
    }

}
