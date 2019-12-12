using System;
using System.IO;
using Newtonsoft.Json;
using appbox.Data;
using appbox.Caching;
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
        private static readonly byte[] RequireArgsPropertyName = System.Text.Encoding.UTF8.GetBytes("A");

        /// <summary>
        /// 读取调用请求的消息头，不读取参数，仅用于WebSocket通道
        /// </summary>
        /// <returns>消息头至参数数组开始消耗的字节数，-1表示空参数列表</returns>
        /// <remarks>目前实现只从第一包读取，即消息头长度不能超过一包的长度</remarks>
        internal static int ReadRequireHead(BytesSegment first, ref int id, ref string service)
        {
            if (first.First != first)
                throw new ArgumentException(nameof(first));

            var jr = new Utf8JsonReader(first.Buffer.AsSpan());

            if (!jr.Read() || jr.TokenType != JsonTokenType.StartObject)
                throw RequireFormatException;
            //I property
            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName
                || !jr.ValueSpan.SequenceEqual(RequireIdPropertyName.AsSpan()))
                throw RequireFormatException;
            if (!jr.Read())
                throw RequireFormatException;
            id = jr.GetInt32();
            //S property
            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName
                || !jr.ValueSpan.SequenceEqual(RequireServicePropertyName.AsSpan()))
                throw RequireFormatException;
            if (!jr.Read())
                throw RequireFormatException;
            service = jr.GetString();
            //A property
            if (!jr.Read() || jr.TokenType != JsonTokenType.PropertyName
                || !jr.ValueSpan.SequenceEqual(RequireArgsPropertyName.AsSpan()))
                throw RequireFormatException;
            if (!jr.Read() || jr.TokenType != JsonTokenType.StartArray)
                throw RequireFormatException;
            //注意预先判断参数数组是否为空
            int arrtyStartIndex = (int)jr.TokenStartIndex; //注意返回指向参数数组开始,非jr.BytesConsumed
            if (jr.Read() && jr.TokenType == JsonTokenType.EndArray)
                return -1;
            return arrtyStartIndex;
        }

    }

}