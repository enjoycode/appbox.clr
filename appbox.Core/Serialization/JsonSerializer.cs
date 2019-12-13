using System;
using System.IO;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using appbox.Data;
using appbox.Models;

namespace appbox.Serialization
{

    public static class JsonExtensions
    {
        private static readonly object EndFlag = new object();
        private static readonly byte[] RefPropertyName = System.Text.Encoding.UTF8.GetBytes("$R");
        private static readonly byte[] TypePropertyName = System.Text.Encoding.UTF8.GetBytes("$T");
        private static readonly byte[] IdPropertyName = System.Text.Encoding.UTF8.GetBytes("Id");

        #region ====Serialize====
        public static void Serialize(this Utf8JsonWriter writer, object res, WritedObjects objrefs)
        {
            if (res == null || res == DBNull.Value)
            {
                writer.WriteNullValue();
                return;
            }

            //先检查引用类型是否已序列化过
            var type = res.GetType();
            bool isRefObject = type.IsClass && type != typeof(string);
            if (isRefObject)
            {
                if (objrefs.TryGetValue(res, out string refID))
                {
                    if (!string.IsNullOrEmpty(refID))
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName(RefPropertyName);
                        writer.WriteStringValue(refID);
                        writer.WriteEndObject();
                        return;
                    }
                    throw new Exception("Json序列化存在重复引用");
                }
                if (res is Entity entity && !entity.Model.IsDTO)
                {
                    refID = $"{(byte)entity.PersistentState}{entity.ModelId}{entity.Id}";
                }
                objrefs.Add(res, refID);
            }

            if (res is IJsonSerializable serializable)
            {
                //开始写入对象
                if (serializable.JsonPayloadType != PayloadType.JsonResult)
                    writer.WriteStartObject();
                if (serializable.JsonPayloadType != PayloadType.UnknownType && serializable.JsonPayloadType != PayloadType.JsonResult)
                {
                    writer.WritePropertyName(TypePropertyName); // 写入对象类型
                    if (serializable.JsonPayloadType == PayloadType.Entity) //实体类写入的是持久化状态及模型标识
                    {
                        var entity = (Entity)res;
                        writer.WriteStringValue($"{(byte)entity.PersistentState}{entity.ModelId}");
                        if (entity.Model.SysStoreOptions != null)
                        {
                            writer.WritePropertyName(IdPropertyName); // 写入对象标识
                            writer.WriteStringValue((Guid)entity.Id);
                        }
                        else if (entity.Model.SqlStoreOptions != null)
                        {
                            //TODO:写入$Id标识，从WriteObjectRefs内累加标识计数器
                        }
                    }
                    else if (serializable.JsonPayloadType == PayloadType.ExtKnownType) //扩展类型写入反射信息
                    {
                        writer.WriteStringValue(type.FullName);
                    }
                    else
                    {
                        writer.WriteNumberValue((int)serializable.JsonPayloadType);
                    }
                }

                // 开始调用实现
                serializable.WriteToJson(writer, objrefs);
                //结束写入对象
                if (serializable.JsonPayloadType != PayloadType.JsonResult)
                    writer.WriteEndObject();
            }
            else //----非实现了IJsonSerializable的对象---
            {
                if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime)
                    || type == typeof(Guid) || type == typeof(decimal))
                {
                    System.Text.Json.JsonSerializer.Serialize(writer, res, type);
                }
                else if (res is IList) // 优先转换IList集合
                {
                    WriteList(writer, (IList)res, objrefs);
                }
                else if (res is IEnumerable) // 其他集合
                {
                    WriteEnumerable(writer, (IEnumerable)res, objrefs);
                }
                else // 转换其他类型如匿名类和没有继承IJsonSerializable接口的类
                {
                    //todo:***** 暂反射简单实现,另考虑缓存
                    var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    writer.WriteStartObject();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        writer.WritePropertyName(properties[i].Name);
                        writer.Serialize(properties[i].GetValue(res), objrefs);
                    }
                    writer.WriteEndObject();
                }
            }
        }

        public static void WriteList(this Utf8JsonWriter writer, IList list, WritedObjects objrefs)
        {
            writer.WriteStartArray();
            for (int i = 0; i < list.Count; i++)
            {
                writer.Serialize(list[i], objrefs);
            }
            writer.WriteEndArray();
        }

        public static void WriteEnumerable(this Utf8JsonWriter writer, IEnumerable list, WritedObjects objrefs)
        {
            writer.WriteStartArray();
            foreach (var item in list)
            {
                writer.Serialize(item, objrefs);
            }
            writer.WriteEndArray();
        }

        public static void WriteEmptyArray(this Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
        #endregion

        #region ====Deserialize====
        public static object Deserialize(this ref Utf8JsonReader reader, ReadedObjects objrefs)
        {
            object res = null;
            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartArray:
                        var array = new ObjectArray();
                        reader.ReadList(array, objrefs);
                        if (array.Count > 0)
                            res = array;
                        break;
                    case JsonTokenType.StartObject:
                        res = reader.ReadObject(objrefs);
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        res = EndFlag;
                        break;
                    case JsonTokenType.String: res = reader.GetString(); break;
                    case JsonTokenType.True: res = true; break;
                    case JsonTokenType.False: res = false; break;
                    case JsonTokenType.Null: break;
                    case JsonTokenType.Number: res = reader.GetDecimal(); break;
                    default: throw new NotImplementedException(reader.TokenType.ToString());
                }
            }
            return res;
        }

        /// <summary>
        /// 注意仅适用于已读取StartObject标记后
        /// </summary>
        internal static object ReadObject(this ref Utf8JsonReader reader, ReadedObjects objrefs)
        {
            if (!reader.Read())
                throw new Exception("ReadObject format error"); //只有{开始标记，没有后续内容
            if (reader.TokenType == JsonTokenType.EndObject) //空对象
                return null;

            //read property name
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new Exception("ReadObject property name error.");
            string propName = reader.GetString(); //TODO:优化比较
            if (propName == "$R") //引用对象
            {
                if (!reader.Read())
                    throw new Exception("ReadObject refid error");
                var refID = reader.GetString();
                if (!reader.Read() && reader.TokenType != JsonTokenType.EndObject) //注意：必须读取EndObject标记
                    throw new Exception("ReadObject object ref format error");
                return objrefs[refID];
            }
            if (propName == "$T")
            {
                //先读取类型信息
                if (!reader.Read())
                    throw new Exception("ReadObject payload type error");
                object typeInfo = reader.TokenType == JsonTokenType.String ?
                    reader.GetString() : (object)reader.GetInt32();

                //根据类型信息创建对象
                IJsonSerializable res = null;
                if (typeInfo is string modelAndState) //表明是实体类或扩展类型
                {
                    if (string.IsNullOrEmpty(modelAndState))
                        throw new Exception("ReadObject type empty.");

                    if (char.IsDigit(modelAndState[0])) //是实体类型
                    {
                        var persistentState = (modelAndState[0]) switch
                        {
                            '0' => PersistentState.Detached,
                            '1' => PersistentState.Unchanged,
                            '2' => PersistentState.Modified,
                            '3' => PersistentState.Deleted,
                            _ => throw new Exception("ReadObject entity persistent state error."),
                        };
                        //先检查实体是否已经读取
                        var modelID = ulong.Parse(modelAndState.AsSpan(1));
                        var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(modelID).Result;
                        if (model.SysStoreOptions != null)
                        {
                            //再读取对象ID
                            if (!reader.Read() && reader.TokenType != JsonTokenType.PropertyName)
                                throw new Exception("ReadObject id property error");
                            if (!reader.Read())
                                throw new Exception("ReadObject id value error");
                            var objId = reader.GetGuid();
                            var refID = $"{modelAndState}{objId}";
                            //注意：先判断objrefs已读字典表是否存在，因为EntitySet可能已加载
                            if (objrefs.TryGetValue(refID, out object existed))
                                res = (Entity)existed;
                            else
                                res = new Entity(model, objId) { PersistentState = persistentState };
                            //加入已读字典表
                            objrefs[refID] = res; //注意：不能用Add,原因同上
                        }
                        else if (model.SqlStoreOptions != null)
                        {
                            //TODO:暂简单处理，未处理引用
                            res = new Entity(model) { PersistentState = persistentState };
                        }
                    }
                    else //扩展类型，暂通过反射创建
                    {
                        var type = Type.GetType(modelAndState, true, true);
                        res = (IJsonSerializable)Activator.CreateInstance(type);
                    }
                }
                else //其他已知类型
                {
                    var payloadType = (PayloadType)((int)typeInfo);
                    var typeSerializer = BinSerializer.GetSerializer(payloadType);
                    if (typeSerializer == null)
                        throw new Exception("ReadObject can not find TypeSerializer");
                    res = (IJsonSerializable)typeSerializer.Creator();
                }

                //开始填充对象内容
                res.ReadFromJson(ref reader, objrefs);
                return res;
            }
            else
            {
                //todo:转换为JObject
                throw new Exception("ReadObject property name error.");
            }
        }

        public static void ReadList(this ref Utf8JsonReader reader, IList list, ReadedObjects objrefs)
        {
            //注意: 已读取StartArray标记
            do
            {
                object res = reader.Deserialize(objrefs);
                if (res == EndFlag)
                    return;
                list.Add(res);
            } while (true);
        }

        /// <summary>
        /// 读取属性名称并与期望值比较，不同则抛出异常
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectPropertyName"></param>
        public static void ExpectPropertyName(this ref Utf8JsonReader reader, string expectPropertyName)
        {
            if (!reader.Read())
                throw new Exception("Nothing to read");
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new Exception("JsonToken is not property name");

            var name = reader.GetString();
            if (name != expectPropertyName)
                throw new Exception("Not expect property name");
        }

        public static void ExpectToken(this ref Utf8JsonReader reader, JsonTokenType expectToken)
        {
            if (!reader.Read())
                throw new Exception("Nothing to read");
            if (reader.TokenType != expectToken)
                throw new Exception("JsonToken is not expected");
        }
        #endregion
    }

    public sealed class WritedObjects : Dictionary<object, string> { }

    public sealed class ReadedObjects : Dictionary<string, object> { }

}