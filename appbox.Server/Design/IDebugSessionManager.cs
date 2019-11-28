using System;

namespace appbox.Server
{
    /// <summary>
    /// 管理服务调试会话，转发HostMessageDispatcher的调用结果
    /// </summary>
    public interface IDebugSessionManager
    {

        /// <summary>
        /// Host进程收到调试子进程的调用结果后，转发至相应的调试会话
        /// </summary>
        /// <remarks>
        /// 注意：该回调暂直接在EventLoop线程内执行
        /// </remarks>
        void GotInvokeResponse(ulong sessionID, InvokeResponse response);

    }
}
