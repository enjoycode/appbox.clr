using System;
using System.IO;
using System.Runtime.CompilerServices;
using appbox.Caching;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Server
{
    public struct InvokeResponse : IMessage
    {
        public const int CONTENT_OFFSET = 11; //注意第一字节为消息类型

        public InvokeSource Source { get; private set; }
        public InvokeProtocol Protocol { get; private set; }
        /// <summary>
        /// 异步等待句柄
        /// </summary>
        public IntPtr WaitHandle { get; private set; }
        public int SourceMsgId { get; private set; }

        public InvokeResponseError Error { get; private set; }
        public AnyValue Result { get; private set; }

        public MessageType Type => MessageType.InvokeResponse;

        public PayloadType PayloadType => PayloadType.InvokeResponse;

        public InvokeResponse(InvokeSource source, InvokeProtocol protocol,
                              IntPtr waitHandle, int srcMsgId, AnyValue result)
        {
            Source = source;
            Protocol = protocol;
            WaitHandle = waitHandle;
            SourceMsgId = srcMsgId;
            Error = InvokeResponseError.None;
            Result = result;
        }

        public InvokeResponse(InvokeSource source, InvokeProtocol contentType,
                              IntPtr waitHandle, int srcMsgId,
                              InvokeResponseError error, string errorMsg)
        {
            Source = source;
            Protocol = contentType;
            WaitHandle = waitHandle;
            SourceMsgId = srcMsgId;
            Error = error;
            Result = AnyValue.From(errorMsg);
        }

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write((byte)Source);
            bs.Write((byte)Protocol);
            long hv = WaitHandle.ToInt64();
            unsafe
            {
                var span = new Span<byte>(&hv, 8);
                bs.Stream.Write(span);
            }
            //注意: 根据类型不同的序列化方式，暂只支持Bin及Json
            if (Protocol == InvokeProtocol.Json)
            {
                //注意: 不catch异常，序列化错误由Channel发送处理
                Result.SerializeAsInvokeResponse(bs.Stream, SourceMsgId);
            }
            else
            {
                bs.Write(SourceMsgId);
                bs.Write((byte)Error);
                Result.WriteObject(bs);
            }
        }

        public void ReadObject(BinSerializer bs)
        {
            Source = (InvokeSource)bs.ReadByte();
            Protocol = (InvokeProtocol)bs.ReadByte();
            long hv = 0;
            unsafe
            {
                var span = new Span<byte>(&hv, 8);
                bs.Stream.Read(span);
            }
            WaitHandle = new IntPtr(hv);

            if (Protocol == InvokeProtocol.Bin)
            {
                SourceMsgId = bs.ReadInt32();
                Error = (InvokeResponseError)bs.ReadByte();
                var temp = new AnyValue();
                temp.ReadObject(bs);
                Result = temp; //Don't use Result.ReadObject(bs)
            }
            else
            {
                //注意将子进程已序列化的结果读到缓存块内，缓存块由相应的通道处理并归还
                var stream = (MessageReadStream)bs.Stream;
                BytesSegment cur = null;
                int len;
                while (stream.HasData)
                {
                    var temp = BytesSegment.Rent();
                    len = bs.Stream.Read(temp.Buffer.AsSpan());
                    temp.Length = len;
                    if (cur != null)
                        cur.Append(temp);
                    cur = temp;
                }
                Result = AnyValue.From(cur.First);
            }
        }
        #endregion

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public unsafe static InvokeSource GetSourceFromMessageChunk(MessageChunk* first)
        //{
        //    var dataPtr = MessageChunk.GetDataPtr(first);
        //    return (InvokeSource)dataPtr[1];
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public unsafe static IntPtr GetWaitHandleFromMessageChunk(MessageChunk* first)
        //{
        //    var dataPtr = MessageChunk.GetDataPtr(first);
        //    long* ptr = (long*)(dataPtr + 3);
        //    return new IntPtr(*ptr);
        //}
    }
}
