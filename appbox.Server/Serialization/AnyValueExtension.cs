using System;
using System.IO;
using System.Threading;
using System.Text.Json;
using appbox.Data;
using appbox.Serialization;

namespace appbox.Server
{
    public static class AnyValueExtension
    {

        //[ThreadStatic]
        //private static Utf8JsonWriter jsonWriter;

        private static readonly byte[] ResponseIdPropertyName = System.Text.Encoding.UTF8.GetBytes("I");
        private static readonly byte[] ResponseErrorPropertyName = System.Text.Encoding.UTF8.GetBytes("E");
        private static readonly byte[] ResponseDataPropertyName = System.Text.Encoding.UTF8.GetBytes("D");

        /// <summary>
        /// 将调用服务的结果AnyValue序列化Json
        /// </summary>
        /// <param name="isResponseError">仅适用于子进程</param>
        public static bool SerializeAsInvokeResponse(this AnyValue obj, Stream stream,
            int msgId, bool isResponseError = false)
        {
            try
            {
                using var jw = new Utf8JsonWriter(stream);
                jw.WriteStartObject();
                jw.WriteNumber(ResponseIdPropertyName.AsSpan(), msgId);

                if (isResponseError || (obj.Type == AnyValueType.Object && obj.ObjectValue is Exception))
                {
                    jw.WriteString(ResponseErrorPropertyName.AsSpan(),
                        isResponseError ? (string)obj.ObjectValue : ((Exception)obj.ObjectValue).Message);
                }
                else
                {
                    jw.WritePropertyName(ResponseDataPropertyName.AsSpan());
                    switch (obj.Type)
                    {
                        case AnyValueType.Empty: jw.WriteNullValue(); break;
                        case AnyValueType.Boolean: jw.WriteBooleanValue(obj.BooleanValue); break;
                        case AnyValueType.Byte: jw.WriteNumberValue(obj.ByteValue); break;
                        case AnyValueType.Int16: jw.WriteNumberValue(obj.Int16Value); break;
                        case AnyValueType.UInt16: jw.WriteNumberValue(obj.UInt16Value); break;
                        case AnyValueType.Int32: jw.WriteNumberValue(obj.Int32Value); break;
                        case AnyValueType.UInt32: jw.WriteNumberValue(obj.UInt32Value); break;
                        case AnyValueType.Float: jw.WriteNumberValue(obj.FloatValue); break;
                        case AnyValueType.Double: jw.WriteNumberValue(obj.DoubleValue); break;
                        case AnyValueType.Decimal: jw.WriteNumberValue(obj.DecimalValue); break;
                        //暂Int64 & UInt64转换为字符串
                        case AnyValueType.Int64: jw.WriteStringValue(obj.Int64Value.ToString()); break;
                        case AnyValueType.UInt64: jw.WriteStringValue(obj.UInt64Value.ToString()); break;

                        case AnyValueType.DateTime: jw.WriteStringValue(obj.DateTimeValue); break;
                        case AnyValueType.Guid: jw.WriteStringValue(obj.GuidValue); break;
                        case AnyValueType.Object:
                            var objrefs = new WritedObjects();
                            jw.Serialize(obj.ObjectValue, objrefs);
                            break;
                    }
                }

                jw.WriteEndObject();
                jw.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"Serialize error: {ExceptionHelper.GetExceptionDetailInfo(ex)}");
                return false;
            }
        }
    }
}
