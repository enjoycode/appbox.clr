using System;
using System.IO;
using Newtonsoft.Json;
using appbox.Data;
using appbox.Serialization;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;

namespace appbox.Server.Channel
{

    static class InvokeHelper //TODO: rename to JsonInvokeHelper
    {

        private static readonly Exception RequireFormatException = new Exception("请求格式错误");

        private static readonly byte[] RequireIdPropertyName = System.Text.Encoding.UTF8.GetBytes("I");
        private static readonly byte[] RequireServicePropertyName = System.Text.Encoding.UTF8.GetBytes("S");

        /// <summary>
        /// 读取调用请求的消息头，不读取参数，仅用于WebSocket通道
        /// </summary>
        /// <returns>消耗的字节数</returns>
        internal static int ReadRequireHead(WebSocketFrame first, ref int id, ref string service)
        {
            var jr = new Utf8JsonReader(first.Buffer.AsSpan());

            if (!jr.Read() || jr.TokenType != JsonTokenType.StartObject)
                throw RequireFormatException;

            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName
                || !jr.ValueSpan.SequenceEqual(RequireIdPropertyName.AsSpan()))
                throw RequireFormatException;
            id = jr.GetInt32();

            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName
                || !jr.ValueSpan.SequenceEqual(RequireServicePropertyName.AsSpan()))
                throw RequireFormatException;
            service = jr.GetString();
            //TODO:考虑读到参数数组开始
            //if (string.IsNullOrEmpty(service))
            //    throw RequireFormatException;
            //if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "A")
            //    throw RequireFormatException;
            return (int)jr.BytesConsumed;
        }

        /// <summary>
        /// 读取并解析为调用请求
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="service"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static void ReadInvokeRequire(TextReader tr, ref int id, ref string service, ref InvokeArgs args)
        {
            var jr = new JsonTextReader(tr);
            jr.DateTimeZoneHandling = DateTimeZoneHandling.Local; //注意时区
            if (!jr.Read() || jr.TokenType != JsonToken.StartObject)
                throw RequireFormatException;
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "I")
                throw RequireFormatException;
            id = jr.ReadAsInt32().Value;
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "S")
                throw RequireFormatException;
            service = jr.ReadAsString();
            if (string.IsNullOrEmpty(service))
                throw RequireFormatException;
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "A")
                throw RequireFormatException;
            var array = jr.Deserialize() as ObjectArray; //TODO:优化防止boxing
            if (array != null && array.Count > 0)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    args.Set(i, AnyValue.From(array[i]));
                }
            }
            jr.Close();
        }

        internal static bool WriteInvokeResponse(StreamWriter sw, int msgId, object res)
        {
            bool serializeError = false;
            var jw = new JsonTextWriter(sw);
            jw.DateTimeZoneHandling = DateTimeZoneHandling.Local;
            var exception = res as Exception;
            try
            {
                if (exception != null)
                {
                    jw.WriteStartObject();
                    jw.WritePropertyName("I");
                    jw.WriteValue(msgId);
                    jw.WritePropertyName("E");
                    jw.WriteValue(exception.Message); //TODO: 友好错误信息
                    jw.WriteEndObject();
                    jw.Close();
                }
                else
                {
                    jw.WriteStartObject();
                    jw.WritePropertyName("I");
                    jw.WriteValue(msgId);
                    jw.WritePropertyName("D");
                    jw.Serialize(res);
                    jw.WriteEndObject();
                }
            }
            catch (Exception ex)
            {
                //TODO:考虑发送序列化错误信息
                serializeError = true;
                Log.Warn($"Serialize Error: {ExceptionHelper.GetExceptionDetailInfo(ex)}");
            }

            jw.Close();
            return serializeError;
        }

        /// <summary>
        /// 发送应用进程已序列化好的结果, 用于AjaxChannel
        /// </summary>
        internal static unsafe void SendInvokeResponse(Stream stream, IntPtr resPtr)
        {
            MessageChunk* cur = (MessageChunk*)resPtr.ToPointer();
            MessageChunk* nxt = null;
            Span<byte> span;
            byte* dataPtr = null;
            try
            {
                while (cur != null)
                {
                    nxt = cur->Next;
                    dataPtr = MessageChunk.GetDataPtr(cur);
                    if (cur == cur->First) //注意第一包跳过InvokeResponse头部至结果部分
                    {
                        span = new Span<byte>(dataPtr + InvokeResponse.CONTENT_OFFSET, cur->DataLength - InvokeResponse.CONTENT_OFFSET);
                    }
                    else
                    {
                        span = new Span<byte>(dataPtr, cur->DataLength);
                    }
                    stream.Write(span); //写入目标
                    cur = nxt;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"写入调用结果至WebApi错误: {ex.Message}");
            }
            finally
            {
                Host.ChildProcess.AppContainer.Channel.ReturnMessageChunks((MessageChunk*)resPtr.ToPointer()); //TODO:暂直接归还
            }
        }

        /// <summary>
        /// 发送服务域已序列化好的结果, 用于WebSocketChannel，由调用者加发送锁
        /// </summary>
        internal static async Task SendInvokeResponse(WebSocket socket, IntPtr resPtr)
        {
            //TODO:暂简单Copy发送
            //var buffer = System.Buffers.MemoryPool<byte>.Shared.Rent(MessageChunk.PayloadDataSize);
            var buffer = WebSocketFrame.Pop();

            IntPtr cur = resPtr;
            try
            {
                while (cur != IntPtr.Zero)
                {
                    cur = CopyMessageChunkToBuffer(cur, buffer);
                    await socket.SendAsync(new ArraySegment<byte>(buffer.Buffer, 0, buffer.Length),
                                           WebSocketMessageType.Text, cur == IntPtr.Zero, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"写入调用结果至WebSocket错误: {ex.Message}");
            }
            finally
            {
                unsafe
                {
                    Host.ChildProcess.AppContainer.Channel.ReturnMessageChunks((MessageChunk*)resPtr.ToPointer()); //TODO:暂直接归还
                }
                //buffer.Dispose();
                WebSocketFrame.Push(buffer);
            }
        }

        private static IntPtr CopyMessageChunkToBuffer(IntPtr msgPtr, WebSocketFrame buffer)
        {
            IntPtr next = IntPtr.Zero;
            int dataSize = 0;
            unsafe
            {
                MessageChunk* cur = (MessageChunk*)msgPtr.ToPointer();
                Span<byte> span;
                byte* dataPtr = MessageChunk.GetDataPtr(cur);
                if (cur == cur->First) //注意第一包跳过InvokeResponse头部至结果部分
                {
                    dataSize = cur->DataLength - InvokeResponse.CONTENT_OFFSET;
                    span = new Span<byte>(dataPtr + InvokeResponse.CONTENT_OFFSET, dataSize);
                }
                else
                {
                    dataSize = cur->DataLength;
                    span = new Span<byte>(dataPtr, dataSize);
                }
                //span.CopyTo(buffer.Memory.Span);
                span.CopyTo(buffer.Buffer);
                buffer.Length = dataSize;
                next = new IntPtr(cur->Next);
            }
            return next;
        }

    }

}