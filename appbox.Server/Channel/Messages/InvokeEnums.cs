using System;

namespace appbox.Server
{
    public enum InvokeContentType : byte
    {
        Bin = 0,
        Json = 1
    }

    /// <summary>
    /// 调用请求的来源
    /// </summary>
    public enum InvokeSource : byte
    {
        Client = 0,
        Host,
        AppContainer,
        Debugger
    }

    public enum InvokeResponseError : byte
    {
        None = 0,
        /// <summary>
        /// 反序列化请求错误
        /// </summary>
        DeserializeRequestFail,
        ServiceNotExists,
        /// <summary>
        /// 服务内部错误
        /// </summary>
        ServiceInnerError,
        SessionNotExisted,
        /// <summary>
        /// 序列化结果错误
        /// </summary>
        SerializeResponseFail,
    }
}
