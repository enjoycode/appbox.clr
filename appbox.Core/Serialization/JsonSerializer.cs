using System;
using System.IO;
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

        #region ====序列化====
        public static void Serialize(this JsonTextWriter writer, object obj, WritedObjects objrefs = null)
        {
            if (objrefs == null)
                objrefs = new WritedObjects();

            WriteCore(writer, obj, objrefs);
        }

        private static void WriteCore(JsonTextWriter writer, object res, WritedObjects objrefs)
        {
            if (res == null || res == DBNull.Value)
            {
                writer.WriteNull();
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
                        writer.WritePropertyName("$R");
                        writer.WriteValue(refID);
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
                    writer.WritePropertyName("$T"); // 写入对象类型
                    if (serializable.JsonPayloadType == PayloadType.Entity) //实体类写入的是持久化状态及模型标识
                    {
                        var entity = (Entity)res;
                        writer.WriteValue($"{(byte)entity.PersistentState}{entity.ModelId}");
                        if (entity.Model.SysStoreOptions != null)
                        {
                            writer.WritePropertyName("Id"); // 写入对象标识
                            writer.WriteValue((Guid)entity.Id);
                        }
                        else if (entity.Model.SqlStoreOptions != null)
                        {
                            //TODO:写入$Id标识，从WriteObjectRefs内累加标识计数器
                        }
                    }
                    else if (serializable.JsonPayloadType == PayloadType.ExtKnownType) //扩展类型写入反射信息
                    {
                        writer.WriteValue(type.FullName);
                    }
                    else
                    {
                        writer.WriteValue((int)serializable.JsonPayloadType);
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
                if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(decimal))
                {
                    writer.WriteValue(res);
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
                        writer.Serialize(properties[i].GetValue(res));
                    }
                    writer.WriteEndObject();
                }
            }
        }

        public static void WriteList(this JsonTextWriter writer, IList list, WritedObjects objrefs)
        {
            writer.WriteStartArray();
            for (int i = 0; i < list.Count; i++)
            {
                WriteCore(writer, list[i], objrefs);
            }
            writer.WriteEndArray();
        }

        public static void WriteEnumerable(this JsonTextWriter writer, IEnumerable list, WritedObjects objrefs)
        {
            writer.WriteStartArray();
            foreach (var item in list)
            {
                WriteCore(writer, item, objrefs);
            }
            writer.WriteEndArray();
        }
        #endregion

        #region ====反序列化====
        public static object Deserialize(this JsonTextReader reader, ReadedObjects objrefs = null)
        {
            if (objrefs == null)
                objrefs = new ReadedObjects();

            return ReadCore(reader, objrefs);
        }

        private static object ReadCore(JsonTextReader reader, ReadedObjects objrefs)
        {
            object res = null;
            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartArray:
                        var array = new ObjectArray();
                        ReadList(reader, array, objrefs);
                        if (array.Count > 0)
                            res = array;
                        break;
                    case JsonToken.StartObject:
                        res = ReadObject(reader, objrefs);
                        break;
                    case JsonToken.EndObject:
                    case JsonToken.EndArray:
                        res = EndFlag;
                        break;
                    default:
                        res = reader.Value;
                        break;
                }
            }
            return res;
        }

        private static object ReadObject(JsonTextReader reader, ReadedObjects objrefs)
        {
            //注意: 已读取StartObject标记
            if (!reader.Read())
                throw new Exception("ReadObject format error"); //只有{开始标记，没有后续内容
            if (reader.TokenType == JsonToken.EndObject) //空对象
                return null;

            //read property name
            if (reader.TokenType != JsonToken.PropertyName)
                throw new Exception("ReadObject property name error.");
            string propName = (string)reader.Value;
            if (propName == "$R") //引用对象
            {
                var refID = reader.ReadAsString();
                if (!reader.Read() && reader.TokenType != JsonToken.EndObject) //注意：必须读取EndObject标记
                    throw new Exception("ReadObject object ref format error");
                return objrefs[refID];
            }
            else if (propName == "$T")
            {
                //先读取类型信息
                if (!reader.Read())
                    throw new Exception("ReadObject payload type error");
                var typeInfo = reader.Value;

                //根据类型信息创建对象
                IJsonSerializable res = null;
                if (typeInfo is string modelAndState) //表明是实体类或扩展类型
                {
                    if (string.IsNullOrEmpty(modelAndState))
                        throw new Exception("ReadObject type empty.");

                    if (char.IsDigit(modelAndState[0])) //是实体类型
                    {
                        PersistentState persistentState;
                        switch (modelAndState[0])
                        {
                            case '0': persistentState = PersistentState.Detached; break;
                            case '1': persistentState = PersistentState.Unchanged; break;
                            case '2': persistentState = PersistentState.Modified; break;
                            case '3': persistentState = PersistentState.Deleted; break;
                            default:
                                throw new Exception("ReadObject entity persistent state error.");
                        }
                        //先检查实体是否已经读取
                        var modelID = ulong.Parse(modelAndState.Substring(1));
                        var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(modelID).Result;
                        if (model.SysStoreOptions != null)
                        {
                            //再读取对象ID
                            if (!reader.Read() && reader.TokenType != JsonToken.PropertyName)
                                throw new Exception("ReadObject id error");
                            var idString = reader.ReadAsString();
                            var refID = $"{modelAndState}{idString}";
                            //注意：先判断objrefs已读字典表是否存在，因为EntitySet可能已加载
                            if (objrefs.TryGetValue(refID, out object existed))
                                res = (Entity)existed;
                            else
                                res = new Entity(model, Guid.Parse(idString)) { PersistentState = persistentState };
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
                res.ReadFromJson(reader, objrefs);
                return res;
            }
            else
            {
                //todo:转换为JObject
                throw new Exception("ReadObject property name error.");
            }
        }

        public static void ReadList(this JsonTextReader reader, IList list, ReadedObjects objrefs)
        {
            //注意: 已读取StartArray标记
            do
            {
                object res = ReadCore(reader, objrefs);
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
        public static void ExpectPropertyName(this JsonTextReader reader, string expectPropertyName)
        {
            if (!reader.Read())
                throw new Exception("Nothing to read");
            if (reader.TokenType != JsonToken.PropertyName)
                throw new Exception("JsonToken is not property name");

            var name = reader.Value as string;
            if (name != expectPropertyName)
                throw new Exception("Not expect property name");
        }

        public static void ExpectToken(this JsonTextReader reader, JsonToken expectToken)
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