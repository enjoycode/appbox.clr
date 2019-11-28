using System;
using System.Collections;
using System.IO;
using appbox.Models;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Data
{
    partial class Entity : IJsonSerializable
    {

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.Entity;

        void IJsonSerializable.WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            var model = Model;
            EntityMemberModel memberModel = null;

            for (int i = 0; i < model.Members.Count; i++)
            {
                memberModel = model.Members[i];
                ref EntityMember m = ref GetMember(memberModel.Name);
                switch (memberModel.Type)
                {
                    case EntityMemberType.EntityRef:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(memberModel.Name);
                            writer.Serialize(m.ObjectValue, objrefs);
                        }
                        break;
                    case EntityMemberType.EntitySet:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(memberModel.Name);
                            writer.WriteList((IList)m.ObjectValue, objrefs);
                        }
                        break;
                    case EntityMemberType.EntityRefDisplayText:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(memberModel.Name);
                            writer.WriteValue((string)m.ObjectValue);
                        }
                        break;
                    case EntityMemberType.DataField:
                        writer.WritePropertyName(memberModel.Name);
                        if (m.HasValue)
                        {
                            switch (m.ValueType)
                            {
                                case EntityFieldType.Binary:
                                    writer.WriteValue((byte[])m.ObjectValue); break;
                                case EntityFieldType.Boolean:
                                    writer.WriteValue(m.BooleanValue); break;
                                case EntityFieldType.Byte:
                                    writer.WriteValue(m.ByteValue); break;
                                case EntityFieldType.DateTime:
                                    writer.WriteValue(m.DateTimeValue); break;
                                case EntityFieldType.Decimal:
                                    writer.WriteValue(m.DecimalValue); break;
                                case EntityFieldType.Double:
                                    writer.WriteValue(m.DoubleValue); break;
                                case EntityFieldType.Enum:
                                    writer.WriteValue(m.Int32Value); break;
                                case EntityFieldType.Float:
                                    writer.WriteValue(m.FloatValue); break;
                                case EntityFieldType.Guid:
                                    writer.WriteValue(m.GuidValue); break;
                                case EntityFieldType.EntityId:
                                    writer.WriteValue((Guid)((EntityId)m.ObjectValue)); break;
                                case EntityFieldType.Int32:
                                    writer.WriteValue(m.Int32Value); break;
                                case EntityFieldType.UInt64:
                                    writer.WriteValue(m.UInt64Value.ToString()); break; //暂序列化为字符串
                                case EntityFieldType.String:
                                    writer.WriteValue((string)m.ObjectValue); break;
                                default: throw new NotSupportedException();
                            }
                        }
                        else
                        {
                            writer.WriteNull();
                        }
                        break;
                    default:
                        throw ExceptionHelper.NotImplemented();
                        //writer.WritePropertyName(m.Name);
                        //writer.Serialize(m.BoxedValue);
                        //break;
                }
            }

            //输出附加成员
            if (_attached != null && _attached.Count > 0)
            {
                foreach (var key in _attached.Keys)
                {
                    writer.WritePropertyName(key);
                    writer.WriteValue(_attached[key]);
                }
            }
        }

        void IJsonSerializable.ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            //注意：json反序列化实体成员一律视为已变更
            var model = Model;
            EntityMemberModel memberModel = null;
            string propName = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    return;

                propName = (string)reader.Value;
                memberModel = model.GetMember(propName, false);
                if (memberModel == null) //表示附加成员或EntitySet已删除集合
                {
                    //TODO: impl it
                    reader.Skip();
                    Log.Warn("Entity json反序化读取附加成员暂未实现");
                }

                switch (memberModel.Type)
                {
                    case EntityMemberType.EntityRef:
                        {
                            //TODO:考虑继承的Base实例是否需要处理，暂直接丢弃
                            var obj = reader.Deserialize(objrefs);
                            SetEntityRef(memberModel.MemberId, (Entity)obj);
                        }
                        break;
                    case EntityMemberType.EntitySet:
                        throw ExceptionHelper.NotImplemented();
                    //{
                    //    //如果非新建状态的实体，则先加载子集并加入ReadedObjects字典表内
                    //    if (PersistentState == PersistentState.Detached)
                    //        this.InitEntitySetLoad(member.Name);
                    //    EntityList oldList = this.GetEntitySetValue(member.Name); //非新建的加载子集
                    //    for (int i = 0; i < oldList.Count; i++) //加入已读字典表防止JsonSerializer再次新建
                    //        objrefs.Add($"{(byte)oldList[i].PersistentState}{oldList[i].ModelID}{oldList[i].ID}", oldList[i]);

                    //    //再反序列化前端传回的ObjectArray数组
                    //    var newList = (ObjectArray)reader.Deserialize(objrefs);
                    //    //最后处理移除被前端删除的对象以及前端添加的对象
                    //    if (newList == null)
                    //    {
                    //        oldList.Clear(); //清除旧的所有
                    //    }
                    //    else
                    //    {
                    //        for (int i = oldList.Count - 1; i >= 0; i--)
                    //        {
                    //            bool foundInNewList = false;
                    //            for (int j = 0; j < newList.Count; j++)
                    //            {
                    //                if (((Entity)newList[j]).ID == oldList[i].ID)
                    //                {
                    //                    foundInNewList = true;
                    //                    break;
                    //                }
                    //            }
                    //            if (!foundInNewList)
                    //                oldList.RemoveAt(i);
                    //        }
                    //        for (int i = 0; i < newList.Count; i++)
                    //        {
                    //            Entity entity = (Entity)newList[i];
                    //            if (entity.PersistentState == PersistentState.Detached)
                    //                oldList.Add(entity);
                    //        }
                    //    }
                    //}
                    //break;
                    case EntityMemberType.DataField:
                        {
                            switch (((DataFieldModel)memberModel).DataType)
                            {
                                case EntityFieldType.Binary:
                                    SetBytes(memberModel.MemberId, reader.ReadAsBytes(), true); break;
                                case EntityFieldType.Boolean:
                                    SetBooleanNullable(memberModel.MemberId, reader.ReadAsBoolean(), true); break;
                                case EntityFieldType.Byte:
                                    SetByteNullable(memberModel.MemberId, (byte?)reader.ReadAsInt32(), true); break;
                                case EntityFieldType.DateTime:
                                    SetDateTimeNullable(memberModel.MemberId, reader.ReadAsDateTime(), true); break;
                                case EntityFieldType.Decimal:
                                    throw ExceptionHelper.NotImplemented();
                                case EntityFieldType.Float:
                                    SetFloatNullable(memberModel.MemberId, (float)reader.ReadAsDouble(), true); break;
                                case EntityFieldType.Double:
                                    throw ExceptionHelper.NotImplemented();
                                case EntityFieldType.Enum:
                                case EntityFieldType.Int32:
                                    SetInt32Nullable(memberModel.MemberId, reader.ReadAsInt32(), true); break;
                                case EntityFieldType.UInt64:
                                    SetUInt64Nullable(memberModel.MemberId, ulong.Parse(reader.ReadAsString()), true); break;
                                case EntityFieldType.EntityId:
                                    {
                                        var v = reader.ReadAsString();
                                        if (!string.IsNullOrEmpty(v))
                                            SetEntityId(memberModel.MemberId, Guid.Parse(v), true);
                                    }
                                    break;
                                case EntityFieldType.Guid:
                                    {
                                        var v = reader.ReadAsString();
                                        Guid? value = null;
                                        if (!string.IsNullOrEmpty(v))
                                            value = Guid.Parse(v);
                                        SetGuidNullable(memberModel.MemberId, value, true);
                                    }
                                    break;
                                case EntityFieldType.String:
                                    SetString(memberModel.MemberId, reader.ReadAsString(), true); break;
                                default: throw new NotSupportedException($"不支持Json读取实体DataField成员: {memberModel.Type}");
                            }
                        }
                        break;
                    default:
                        reader.Skip(); //TODO:暂忽略所有其他类型
                        break;
                }
            }

            throw new Exception("Can not read EndObjectFlag");
        }

        public override string ToString()
        {
            //TODO:简化
            using (var sw = new StringWriter())
            using (var jw = new JsonTextWriter(sw))
            {
                jw.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                jw.Serialize(this);
                return sw.ToString();
            }
        }
    }
}
