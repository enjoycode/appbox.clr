using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using appbox.Server;
using appbox.Serialization;

namespace appbox.Design
{
    /// <summary>
    /// 用于管理调试会话
    /// </summary>
    sealed class DebugSessionManager : IDebugSessionManager
    {
        internal static readonly DebugSessionManager Instance = new DebugSessionManager();

        private readonly Dictionary<ulong, DebugService> sessions = new Dictionary<ulong, DebugService>();

        /// <summary>
        /// 因为HostMessageDispatcher在appbox.Host组件内，所以使用委托创建
        /// </summary>
        internal static Func<IDebugSessionManager, IMessageDispatcher> DebugMessageDispatcherMaker;

        internal static IMessageDispatcher MakeMessageDispatcher()
        {
            return DebugMessageDispatcherMaker(Instance);
        }

        internal void StartSession(DebugService service)
        {
            lock (sessions)
            {
                sessions[service.Session.SessionID] = service;
            }
        }

        internal void RemoveSession(ulong sessionID)
        {
            lock (sessions)
            {
                sessions.Remove(sessionID);
            }
        }

        public void GotInvokeResponse(ulong sessionID, InvokeResponse response)
        {
            //TODO:考虑线程池执行
            //Log.Debug($"收到调试调用结果: {response.Result}");

            DebugService ds = null;
            lock (sessions)
            {
                sessions.TryGetValue(sessionID, out ds);
            }
            if (ds == null)
            {
                Log.Warn("收到调试调用结果时找不到相应的调试会话");
                return;
            }

            //把结果转发至前端
            string eventBody;
            if (response.Error == InvokeResponseError.None)
            {
                var sb = new System.Text.StringBuilder();
                using (var sw = new System.IO.StringWriter(sb))
                using (var jw = new JsonTextWriter(sw))
                {
                    jw.Serialize(response.Result);
                }
                eventBody = string.Format("{{\"Type\":\"Result\",\"Data\":{0}}}", sb);
            }
            else
            {
                eventBody = string.Format("{{\"Type\":\"Result\",\"Error\":\"{0}\"}}", response.Error);
            }
            ds.ForwardEvent(eventBody);

            //终止调试器进程
            ds.StopDebugger();
        }

        /// <summary>
        /// 将Host进程的存储回调消息转发给相应的调试子进程
        /// </summary>
        public void ForwardStoreMessage(ref NativeMessage msg)
        {
            ulong sessionId = msg.Source;
            DebugService ds = null;
            lock (sessions)
            {
                sessions.TryGetValue(sessionId, out ds);
            }
            if (ds == null)
            {
                Log.Warn("转发存储消息时找不到相应的调试会话");
                return;
            }

            ds.Channel.SendMessage(ref msg);
        }
    }
}
