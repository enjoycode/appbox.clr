using System;
using System.IO;
using System.Runtime.CompilerServices;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Server
{
    public struct InvokeResponse : IMessage
    {
        public const int CONTENT_OFFSET = 11; //注意第一字节为消息类型

        public InvokeSource Source { get; private set; }
        public InvokeContentType ContentType { get; private set; }
        /// <summary>
        /// 异步等待句柄
        /// </summary>
        public IntPtr WaitHandle { get; private set; }
        public int SourceMsgId { get; private set; }

        public InvokeResponseError Error { get; private set; }
        public object Result { get; private set; }

        public MessageType Type => MessageType.InvokeResponse;

        public PayloadType PayloadType => PayloadType.InvokeResponse;

        public InvokeResponse(InvokeSource source, InvokeContentType contentType, IntPtr waitHandle, int srcMsgId, object result)
        {
            Source = source;
            ContentType = contentType;
            WaitHandle = waitHandle;
            SourceMsgId = srcMsgId;
            Error = InvokeResponseError.None;
            Result = result;
        }

        public InvokeResponse(InvokeSource source, InvokeContentType contentType, IntPtr waitHandle, int srcMsgId,
                              InvokeResponseError error, string errorMsg)
        {
            Source = source;
            ContentType = contentType;
            WaitHandle = waitHandle;
            SourceMsgId = srcMsgId;
            Error = error;
            Result = errorMsg;
        }

        public void WriteObject(BinSerializer bs)
        {
            bs.Write((byte)Source);
            bs.Write((byte)ContentType);
            long hv = WaitHandle.ToInt64();
            unsafe
            {
                var span = new Span<byte>(&hv, 8);
                bs.Stream.Write(span);
            }
            //注意: 根据类型不同的序列化方式，暂只支持Bin及Json
            if (ContentType == InvokeContentType.Json)
            {
                using (var sw = new StreamWriter(bs.Stream))
                {
                    //TODO:抽象公共部分至InvokeHelper
                    //注意: 不catch异常，序列化错误由Channel发送处理
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jw.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                        jw.WriteStartObject();
                        jw.WritePropertyName("I");
                        jw.WriteValue(SourceMsgId);
                        if (Error != InvokeResponseError.None)
                        {
                            jw.WritePropertyName("E");
                            jw.WriteValue((string)Result); //TODO: 友好错误信息
                        }
                        else
                        {
                            jw.WritePropertyName("D");
                            jw.Serialize(Result);
                        }
                        jw.WriteEndObject();
                    }
                }
            }
            else
            {
                bs.Write(SourceMsgId);
                bs.Write((byte)Error);
                bs.Serialize(Result);
            }
        }

        public void ReadObject(BinSerializer bs)
        {
            //注意: 只支持Bin，Json由Host直接转发，不需要反序列化
            Source = (InvokeSource)bs.ReadByte();
            ContentType = (InvokeContentType)bs.ReadByte();
            long hv = 0;
            unsafe
            {
                var span = new Span<byte>(&hv, 8);
                bs.Stream.Read(span);
            }
            WaitHandle = new IntPtr(hv);
            SourceMsgId = bs.ReadInt32();
            Error = (InvokeResponseError)bs.ReadByte();
            if (ContentType == InvokeContentType.Bin)
                Result = bs.Deserialize();
            else
                throw new NotSupportedException("暂不支持非二进制格式");
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static InvokeSource GetSourceFromMessageChunk(MessageChunk* first)
        {
            var dataPtr = MessageChunk.GetDataPtr(first);
            return (InvokeSource)dataPtr[1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static IntPtr GetWaitHandleFromMessageChunk(MessageChunk* first)
        {
            var dataPtr = MessageChunk.GetDataPtr(first);
            long* ptr = (long*)(dataPtr + 3);
            return new IntPtr(*ptr);
        }
    }
}
