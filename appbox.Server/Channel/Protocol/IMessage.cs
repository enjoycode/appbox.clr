using System;
using appbox.Serialization;

namespace appbox.Server
{
    public interface IMessage : IBinSerializable
    {
        MessageType Type { get; } //TODO:检查是否能移除

        /// <summary>
        /// 仅用于MessageSerializer
        /// </summary>
        PayloadType PayloadType { get; }
    }

    //public interface IResponseMessage : IMessage
    //{
    //    Exception GetException();

    //    object GetResult();
    //}
}
