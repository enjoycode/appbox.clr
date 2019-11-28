using System;
using appbox.Runtime;

namespace appbox.Design
{
    public interface IDeveloperSession : ISessionInfo
    {
        /// <summary>
        /// 获取当前用户会话的开发者的DesighHub实例
        /// </summary>
        DesignHub GetDesignHub();

        /// <summary>
        /// 发送设计时事件
        /// </summary>
        void SendEvent(int source, string body);
    }
}
