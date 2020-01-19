using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using appbox.Caching;

namespace appbox.Client.Channel
{
    /// <summary>
    /// WebSocket Channel
    /// </summary>
    public sealed class WSChannel : IClientChannel
    {
        private readonly ClientWebSocket socket;
        private readonly string host;
        private int msgIndex; //消息流水号
        private BytesSegment pending;

        private static readonly ObjectPool<PooledTaskSource<object>> waitPool = PooledTaskSource<object>.Create(8);
        private static readonly Dictionary<int, PooledTaskSource<object>> waits
            = new Dictionary<int, PooledTaskSource<object>>();

        public WSChannel(string host)
        {
            this.host = host;
            socket = new ClientWebSocket();
        }

        #region ====Connect & Receive Methods====
        private async Task ConnectAndStartReceiveAsync()
        {
            await socket.ConnectAsync(new Uri($"ws://{host}/wsapi"), CancellationToken.None).ConfigureAwait(false);
            StartReceiveAsync();
        }

        private void StartReceiveAsync()
        {
            Task.Run(async () =>
            {
                ValueWebSocketReceiveResult result;
                BytesSegment frame;
                do
                {
                    frame = BytesSegment.Rent();
                    try
                    {
                        result = await socket.ReceiveAsync(frame.Buffer.AsMemory(), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            BytesSegment.ReturnOne(frame);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        BytesSegment.ReturnOne(frame);
                        Console.WriteLine($"WebSocket receive error: {ex.Message}");
                        //TODO:考虑开始重新连接
                        break;
                    }
                    frame.Length = result.Count;

                    OnReceiveMessage(frame, result.EndOfMessage); //不需要捕获异常
                } while (true);
            });
        }

        private /*async ValueTask*/ void OnReceiveMessage(BytesSegment frame, bool isEnd)
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

                //开始读取消息标识并从挂起请求中查找
                var msgId = ReadMsgIdFromResponse(frame.First);
                if (waits.TryGetValue(msgId, out PooledTaskSource<object> tcs))
                {
                    tcs.SetResult(frame); //注意为最后一包
                }
                else
                {
                    BytesSegment.ReturnAll(frame.First);
                }
            }
        }

        /// <summary>
        /// 从响应的第一包中读取消息Id
        /// </summary>
        private int ReadMsgIdFromResponse(BytesSegment first)
        {
            Debug.Assert(ReferenceEquals(first.First, first));
            var jr = new Utf8JsonReader(first.Memory.Span);
            if (!jr.Read() || jr.TokenType != JsonTokenType.StartObject)
                throw new Exception("Response format error.");
            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName || jr.GetString() != "I")
                throw new Exception("Response format error.");
            if (!jr.Read() || jr.TokenType != JsonTokenType.Number)
                throw new Exception("Response format error.");
            //TODO:读取异常
            return jr.GetInt32();
        }
        #endregion

        #region ====Public IClientChannel Methods====
        public async Task LoginAsync(string user, string pass)
        {
            //临时方案判断socket是否已打开，已打开则关闭，主要用于防止重新登录时服务端WebSocket还绑定旧会话
            //if (socket.State == WebSocketState.Open)
            //    await socket.CloseAsync(WebSocketCloseStatus.Empty, "restart", CancellationToken.None);

            var reqdata = $"{{\"User\":\"{user}\",\"Password\":\"{pass}\"}}";
            var reqContent = new StringContent(reqdata);
            reqContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            reqContent.Headers.ContentType.CharSet = "utf-8";

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync($"http://{host}/api/Login/post", reqContent);
            if (response.IsSuccessStatusCode)
            {
                //判断返回结果是否登录成功
                var resContentStream = await response.Content.ReadAsStreamAsync();
                var loginResult = await JsonSerializer.DeserializeAsync<LoginResult>(resContentStream);
                if (!loginResult.succeed)
                    throw new Exception(loginResult.error);

                if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies))
                {
                    socket.Options.Cookies = new System.Net.CookieContainer();
                    foreach (var cookie in cookies)
                    {
                        socket.Options.Cookies.SetCookies(new Uri($"http://{host}"), cookie);
                    }
                }

                //暂在这里建立WebSocket连接，并开始接收数据
                await ConnectAndStartReceiveAsync();
            }
            else
            {
                Console.WriteLine(response.StatusCode.ToString());
            }
        }

        public async Task<TResult> InvokeAsync<TResult>(string service, string args)
        {
            if (socket.State != WebSocketState.Open)
            {
                //if (socket.State != WebSocketState.Connecting)
                //    await ConnectAndStartReceiveAsync(); //尝试重新连接 
                throw new Exception("Connection is not open");
            }

            var msgId = Interlocked.Increment(ref msgIndex);
            var require = $"{{\"I\":{msgId},\"S\":\"{service}\",\"A\":{args}}}";
            var requireData = System.Text.Encoding.UTF8.GetBytes(require);
            await socket.SendAsync(requireData, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            var tcs = waitPool.Allocate();
            lock (waits)
            {
                waits.Add(msgId, tcs);
            }
            var lastFrame = (BytesSegment)await tcs.WaitAsync();
            lock (waits)
            {
                waits.Remove(msgId);
            }
            waitPool.Free(tcs);

            //反序列化结果
            InvokeResult<TResult> res;
            try
            {
                res = Deserialize<InvokeResult<TResult>>(lastFrame);
            }
            finally
            {
                BytesSegment.ReturnAll(lastFrame.First);
            }
            if (!string.IsNullOrEmpty(res.E))
                throw new Exception(res.E);

            return res.D;
        }

        private static TResult Deserialize<TResult>(BytesSegment last)
        {
            var seqs = new System.Buffers.ReadOnlySequence<byte>(last.First, 0, last, last.Length);
            var jr = new Utf8JsonReader(seqs);
            return JsonSerializer.Deserialize<TResult>(ref jr);
        }
        #endregion

    }
}
