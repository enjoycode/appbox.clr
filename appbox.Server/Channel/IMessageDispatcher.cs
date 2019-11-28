using System;

namespace appbox.Server
{
    /// <summary>
    /// 消息分发处理器，用于分发处理通道收到的完整消息包链
    /// </summary>
    /// <remarks>
    /// 注意：调用时尚在接收线程内，由实现者决定是否利用线程池执行具体的操作
    /// </remarks>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// 处理通道接收的消息，注意: 当前线程为接收Loop
        /// </summary>
        unsafe void ProcessMessage(IMessageChannel channel, MessageChunk* first);
    }
}
