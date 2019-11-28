using System;

namespace appbox.Server
{
    public interface IMessageChannel
    {
        /// <summary>
        /// 目前用于Host进程区分调试子进程
        /// </summary>
        ulong RemoteRuntimeId { get; }

        /// <summary>
        /// 反序列化消息，不管是否成功都归还缓存块
        /// </summary>
        unsafe T Deserialize<T>(MessageChunk* first) where T : struct, IMessage;

        unsafe void ReturnMessageChunks(MessageChunk* first);

        /// <summary>
        /// 序列化并发送消息，如果序列化异常标记消息为错误状态仍旧发送,接收端根据消息类型是请求还是响应作不同处理
        /// </summary>
        void SendMessage<T>(ref T msg) where T : struct, IMessage;
    }
}
