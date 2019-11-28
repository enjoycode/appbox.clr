using System;

namespace appbox.Server
{
    public sealed class MessageSerializationException : Exception
    {
        public MessageSerilizationErrorCode ErrorCode { get; private set; }

        public MessageSerializationException(MessageSerilizationErrorCode errorCode, Exception inner) : base(null, inner)
        {
            ErrorCode = errorCode;
        }
    }

    public enum MessageSerilizationErrorCode
    {
        SerializeFail,

        DeserializeFailByFirstSegmentIsNull,
        DeserializeFail,

        //反序列化消息解密失败
        DeserializeFailByDecrypt,
        DeserializeFailByMessageType
    }
}
