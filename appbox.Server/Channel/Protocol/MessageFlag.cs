using System;

namespace appbox.Server
{
    [Flags]
    public enum MessageFlag : byte
    {
        None = 0,
        Compress = 1,
        Security = 2,
        FirstChunk = 4, //todo: 考虑取消
        LastChunk = 8,
        NoLic = 16,
        /// <summary>
        /// 向通道写入消息时序列化失败，标记消息通知接收端作相应的处理
        /// </summary>
        SerializeError = 32
    }
}
