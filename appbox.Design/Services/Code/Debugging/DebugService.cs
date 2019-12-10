using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using appbox.Data;
using appbox.Serialization;
using appbox.Server;

namespace appbox.Design
{
    /// <summary>
    /// 每个DesignHub对应一个DebugService实例，负责启动Debugger进程并转发输入输出
    /// </summary>
    sealed class DebugService
    {

        //协议部分参考vscode V8Protocol <- RawDebugSession, DebugProtocol

        private readonly DesignHub _hub;
        private int _reqIndex = 0;
        private readonly object _sendLock = new object();
        private readonly object _readLock = new object();
        private Process _process;
        private readonly string _programPath;
        private readonly string _programCWD;
        private readonly Guid _debugSessionID;

        private byte[] _buffer = new byte[1024];
        private int _bufferOffset = 0; //当前写入位置
        private int _headEndIndex = -1;
        private int _bodyLen = -1;
        private int _runningFlag = 0; //0=未启动，1=运行中, 2=暂停中

        private readonly Dictionary<int, Action<string>> _pendingReqs;

        internal IDeveloperSession Session
        {
            get { return (IDeveloperSession)_hub.Session; }
        }

        internal SharedMemoryChannel Channel; //调试子进程的消息通道
        internal string DebugSourcePath; //调试目标服务的源文件路径
        private JArray _breakpoints;
        private int _stopAtStackFrameId; //暂停的线程stackframe标识

        /// <summary>
        /// 是否暂停中
        /// </summary>
        internal bool IsPause
        {
            get { return Thread.VolatileRead(ref _runningFlag) == 2; }
        }

        public DebugService(DesignHub hub)
        {
            _hub = hub;
            _pendingReqs = new Dictionary<int, Action<string>>();
            _debugSessionID = new Guid();
            _programCWD = Runtime.RuntimeContext.Current.AppPath;
            _programPath = Path.Combine(_programCWD, "appbox.AppContainer");
        }

        #region ====Start & Stop Debugger====
        // 原步骤说明: initialize -> launch -> setBreakpoints -> configurationDone -> 其他后续如获取线程等
        internal void StartDebugger(string method, string methodArgs, string breakpoints)
        {
            if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) != 0)
            {
                throw new Exception("Debugger has started.");
            }

            //1. 构建请求消息，读取类似InvokeController的递交的json格式的调用请求
            var args = new InvokeArgs();
            using (var jr = new JsonTextReader(new StringReader(methodArgs)))
            {
                if (jr.Deserialize() is ObjectArray array && array.Count > 0)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        args.Set(i, AnyValue.From(array[i]));
                    }
                }
            }
            var req = new InvokeRequire(InvokeSource.Debugger,
                InvokeProtocol.Bin/*注意用二进制格式，主进程反序列化*/, IntPtr.Zero, method, args, 0, _hub.Session);

            _breakpoints = JArray.Parse(breakpoints);

            //2.建立调试子进程消息通道并注册调试会话
            Channel = new SharedMemoryChannel(_hub.Session.SessionID.ToString(),
                128/*TODO:*/, DebugSessionManager.MakeMessageDispatcher(), _hub.Session.SessionID);
            Channel.StartReceive();
            Channel.SendMessage(ref req); //直接发送调用调试目标的消息

            DebugSessionManager.Instance.StartSession(this);

            //3.再启动调试进程
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(Runtime.RuntimeContext.Current.AppPath, Server.Consts.LibPath, "netcoredbg");
            //注意netcoredbg覆盖启动目标及参数
            process.StartInfo.Arguments = $"--interpreter=vscode -- appbox.AppContainer {Runtime.RuntimeContext.PeerId} {_hub.Session.SessionID}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += OnError;

            _process = process;
            process.Start();
            Task.Run(async () => await ReadOutputAsync()); //ReadOutputAsync();
            process.BeginErrorReadLine();

            //4.开始发送指令链
            Initialize(InitializeCB);
        }

        internal void StopDebugger(bool force = false)
        {
            // 注意：不能暴力终止调试器进程
            if (Thread.VolatileRead(ref _runningFlag) != 0)
            {
                //Log.Warn($"will stop debugger, force={force}");
                try
                {
                    SendRequest("disconnect", "{\"restart\":false}", DisconnectCB);
                }
                catch (Exception ex) //可能调试进程异常关闭
                {
                    Log.Warn($"Send disconnect error: {ex.Message}");
                    DisconnectCB(string.Empty);
                }
            }
        }
        #endregion

        #region ====请求及回调====
        private void Initialize(Action<string> cb)
        {
            const string args = "{\"clientID\":\"vscode\",\"adapterID\":\"coreclr\",\"pathFormat\":\"path\",\"linesStartAt1\":true,\"columnsStartAt1\":true,\"supportsVariableType\":true,\"supportsVariablePaging\":true,\"supportsRunInTerminalRequest\":true,\"locale\":\"en-us\"}";
            SendRequest("initialize", args, cb);
        }

        private void InitializeCB(string body)
        {
            Launch(LaunchCB);
        }

        private void Launch(Action<string> cb)
        {
            //netcoredbg启动调试目标必须通过参数指定，否则默认使用dotnet来启动
            //string programArgs = string.Format("[\"{0}\",\"{1}\"]", Runtime.RuntimeContext.PeerId, sessionID);
            //string args = string.Format("{{\"name\":\"ServiceDebugger\",\"type\":\"coreclr\",\"request\":\"launch\",\"program\":\"{0}\",\"args\":{1},\"cwd\":\"{2}\",\"console\":\"internalConsole\",\"stopAtEntry\":false,\"internalConsoleOptions\":\"openOnSessionStart\",\"__sessionId\":\"{3}\"}}"
            //    , _programPath, programArgs, _programCWD, _debugSessionID);
            string args = string.Format("{{\"name\":\"ServiceDebugger\",\"type\":\"coreclr\",\"request\":\"launch\",\"console\":\"internalConsole\",\"stopAtEntry\":false,\"internalConsoleOptions\":\"openOnSessionStart\",\"__sessionId\":\"{0}\"}}"
                , _debugSessionID);
            //Log.Debug($"Debug.Launch args={args}");
            SendRequest("launch", args, cb);
        }

        private void LaunchCB(string body)
        {
            SetBreakpoints(_breakpoints, SetBreakpointsCB);
        }

        private void SetBreakpoints(JArray bps, Action<string> cb)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{\"source\":{{\"path\":\"{0}\",\"name\":\"{0}\"}}", DebugSourcePath);
            sb.Append(",\"breakpoints\":[");
            for (int i = 0; i < bps.Count(); i++)
            {
                var item = bps[i];
                sb.AppendFormat("{{\"line\":{0}}}", item["line"].Value<int>());
                if (i != bps.Count() - 1)
                    sb.Append(",");
            }
            sb.Append("],\"sourceModified\":false}");
            SendRequest("setBreakpoints", sb.ToString(), SetBreakpointsCB);
        }

        private void SetBreakpointsCB(string body)
        {
            ConfigurationDone();
        }

        private void ConfigurationDone()
        {
            SendRequest("configurationDone", null, null);
        }

        internal void Continue(int threadId)
        {
            var args = string.Format("{{\"threadId\":{0}}}", threadId);
            SendRequest("continue", args, null);
            Interlocked.Exchange(ref _runningFlag, 1); //todo:考虑回调处理
        }

        /// <summary>
        /// 调试器暂停时查询当前Thread的StackFrame，用于取值时传参
        /// 注意：目前只取1个
        /// </summary>
        private void StackTrace(int threadId)
        {
            Interlocked.Exchange(ref _stopAtStackFrameId, 0);
            var args = string.Format("{{\"threadId\":{0},\"startFrame\":0,\"levels\":1}}", threadId);
            SendRequest("stackTrace", args, res =>
            {
                //TODO:fix netcoredbg问题
                var jobj = JObject.Parse(res);
                var jarray = (JArray)jobj["body"]["stackFrames"];
                Interlocked.Exchange(ref _stopAtStackFrameId, jarray[0]["id"].Value<int>());
            });
        }

        internal void Evaluate(string expression, Action<string> cb)
        {
            // context = repl or hover
            //var frameId = Thread.VolatileRead(ref _stopAtStackFrameId);
            //var args = string.Format("{{\"expression\":\"{0}\",\"frameId\":{1},\"context\":\"hover\"}}", expression, frameId);
            var args = string.Format("{{\"expression\":\"{0}\",\"context\":\"hover\"}}", expression);
            SendRequest("evaluate", args, res =>
            {
                var jobj = JObject.Parse(res);
                if (jobj["success"].Value<bool>())
                    cb((string)jobj["body"]["result"]);
                else
                    cb($"表达式:{expression}取值失败" /*+ (string)jobj["message"]*/);
            });
        }

        private void DisconnectCB(string body)
        {
            //先清理资源
            lock (_readLock)
            {
                _bufferOffset = 0;
                _bodyLen = _headEndIndex = -1;
            }
            DebugSessionManager.Instance.RemoveSession(Session.SessionID);

            //再等待进程退出, 注意：有时候调试进程不终止，待深究
            if (!_process.WaitForExit(300))
            {
                _process.Kill();
                _process.WaitForExit();
            }
            _process.Close();
            _process = null;

            Channel.StopReceive();

            Interlocked.Exchange(ref _runningFlag, 0);
            Log.Debug("调试器进程已退出");
        }
        #endregion

        #region ====发送部分====
        private void SendRequest(string command, string args, Action<string> cb)
        {
            lock (_sendLock)
            {
                _reqIndex++;
                var body = string.Format("{{\"command\":\"{0}\",\"arguments\":{1},\"type\":\"request\",\"seq\":{2}}}"
                    , command, string.IsNullOrEmpty(args) ? "{}" : args, _reqIndex);
                int length = Encoding.UTF8.GetByteCount(body);

                //加入挂起请求列表
                if (cb != null)
                {
                    lock (_pendingReqs)
                    {
                        _pendingReqs.Add(_reqIndex, cb);
                    }
                }

                _process.StandardInput.Write(string.Format("Content-Length: {0}\r\n\r\n", length));
                _process.StandardInput.Write(body);
            }
        }

        private void SendResponse(string reqCommand, int reqSeq, bool success, string message, string args)
        {
            lock (_sendLock)
            {
                //暂根据vscode源码seq = 0
                var body = string.Format("{{\"type\":\"response\",\"seq\":0,\"command\":\"{0}\",\"request_seq\":{1},\"success\":{2},\"message\":\"{3}\",\"body\":{4}}}"
                    , reqCommand, reqSeq, success ? "true" : "false", message, string.IsNullOrEmpty(args) ? "{}" : args);
                int length = Encoding.UTF8.GetByteCount(body);

                _process.StandardInput.Write(string.Format("Content-Length: {0}\r\n\r\n", length));
                _process.StandardInput.Write(body);
            }
        }
        #endregion

        #region ====接收部分====
        private static readonly byte[] TwoCRLF = { 13, 10, 13, 10 };
        private static readonly byte[] HeadStart = Encoding.UTF8.GetBytes("Content-Length:");

        private async Task ReadOutputAsync()
        {
            //todo:暂简单实现组包
            var tempBuffer = new byte[512];
            int len = -1;
            while (Thread.VolatileRead(ref _runningFlag) != 0 /*_process != null && !_process.HasExited*/)
            {
                try
                {
                    len = await _process.StandardOutput.BaseStream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
                }
                catch (Exception)
                {
                    len = 0;
                    Log.Warn("读取调试器输出错误");
                }

                if (len > 0)
                {
                    lock (_readLock)
                    {
                        var left = _buffer.Length - _bufferOffset;
                        if (left < len) //空间不够扩容
                        {
                            var newBuffer = new byte[_buffer.Length + len - left];
                            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferOffset);
                            _buffer = newBuffer;
                        }
                        Buffer.BlockCopy(tempBuffer, 0, _buffer, _bufferOffset, len);
                        _bufferOffset += len;

                        // 开始组装消息
                        BuildMessage();
                    }
                }
            }
        }

        private void BuildMessage()
        {
            //先尝试读取完整消息头
            if (_bodyLen < 0)
            {
                _headEndIndex = IndexOfBytes(_buffer, TwoCRLF, 0, _bufferOffset);
                if (_headEndIndex > 0) //完整的消息头
                {
                    //中间部分即是ContentLength的值
                    string lenString = Encoding.UTF8.GetString(_buffer, HeadStart.Length, _headEndIndex - HeadStart.Length);
                    _bodyLen = int.Parse(lenString);
                }
            }

            //判断是否具备完整消息
            if (_bodyLen > 0)
            {
                var dataLen = _bufferOffset - _headEndIndex - 4; //去掉头部后面的数据长度
                if (dataLen >= _bodyLen)
                {
                    var bodyString = Encoding.UTF8.GetString(_buffer, _headEndIndex + 4, _bodyLen);
                    //移动缓存块并重置
                    var leftStart = _headEndIndex + 4 + _bodyLen;
                    Buffer.BlockCopy(_buffer, leftStart, _buffer, 0, _bufferOffset - leftStart);
                    _bufferOffset = _bufferOffset - _headEndIndex - 4 - _bodyLen;
                    _bodyLen = _headEndIndex = -1;
                    //分发消息
                    Dispatch(bodyString);
                    //继续处理剩余部分
                    if (_bufferOffset > 0)
                        BuildMessage();
                }
            }
        }

        private static int IndexOfBytes(byte[] array, byte[] pattern, int startIndex, int count)
        {
            int i = startIndex;
            int endIndex = count > 0 ? startIndex + count : array.Length;
            int fidx = 0;

            while (i++ < endIndex)
            {
                fidx = (array[i] == pattern[fidx]) ? ++fidx : 0;
                if (fidx == pattern.Length)
                {
                    return i - fidx + 1;
                }
            }
            return -1;
        }

        private void Dispatch(string rawBody)
        {
            Log.Debug($"调试器消息:{rawBody}");
            var msg = JObject.Parse(rawBody);
            var type = (string)msg["type"];
            switch (type)
            {
                case "response":
                    Action<string> cb = null;
                    int request_seq = msg["request_seq"].Value<int>();
                    lock (_pendingReqs)
                    {
                        if (_pendingReqs.TryGetValue(request_seq, out cb))
                            _pendingReqs.Remove(request_seq);
                    }
                    cb?.Invoke(rawBody);
                    break;
                case "request":
                    DispatchRequest(rawBody, msg);
                    break;
                case "event":
                    DispatchEvent(rawBody, msg);
                    break;
            }
        }

        private void DispatchRequest(string raw, JObject req)
        {
            var reqCommand = (string)req["command"];
            var reqSeq = req["seq"].Value<int>();
            if (reqCommand == "handshake")
            {
                Log.Error("收到Handshake");
                //var value = (string)req["arguments"]["value"];
                //var sig = GetSig(value);
                //this.SendResponse(reqCommand, reqSeq, true, "", "{\"signature\":\"" + sig + "\"}");
            }
            else
            {
                SendResponse(reqCommand, reqSeq, false, "unknown request " + reqCommand, null);
            }
        }

        private void DispatchEvent(string raw, JObject msg)
        {
            var eventType = (string)msg["event"];
            if (eventType == "stopped" && (string)msg["body"]["reason"] == "breakpoint")
            {
                Interlocked.Exchange(ref _runningFlag, 2);
                int threadID = msg["body"]["threadId"].Value<int>();
                //开始查询线程stack，注意：目前只查询当前stack
                //StackTrace(threadID);

                int line = msg["body"]["line"].Value<int>();
                var eventBody = string.Format("{{\"Type\":\"HitBreakpoint\",\"Thread\":{0},\"Line\": {1}}}", threadID, line);
                ForwardEvent(eventBody);
            }
        }
        #endregion

        #region ====进程事件====
        private void OnError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                Log.Warn($"Debugger error: {e.Data}");
        }
        #endregion

        #region ====转发部分====
        private const int DEBUGGER_EVENT = 2;

        /// <summary>
        /// 将调试器事件消息转发给前端
        /// </summary>
        internal void ForwardEvent(string body)
        {
            Session.SendEvent(DEBUGGER_EVENT, body);
        }
        #endregion

        #region ====IDisposable Support====
        private bool disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopDebugger(true); //用于前端直接关闭浏览器后，断开WebSocket后的强制中止调试进程
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
