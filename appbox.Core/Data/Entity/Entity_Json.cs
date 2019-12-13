using System;
using System.Collections;
using System.IO;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Data
{
    partial class Entity : IJsonSerializable
    {

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.Entity;

        void IJsonSerializable.WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            var model = Model;
            EntityMemberModel mm;
            for (int i = 0; i < model.Members.Count; i++)
            {
                mm = model.Members[i];
                ref EntityMember m = ref GetMember(mm.Name);
                switch (mm.Type)
                {
                    case EntityMemberType.EntityRef:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(mm.Name);
                            writer.Serialize(m.ObjectValue, objrefs);
                        }
                        break;
                    case EntityMemberType.EntitySet:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(mm.Name);
                            writer.WriteList((IList)m.ObjectValue, objrefs);
                        }
                        break;
                    case EntityMemberType.EntityRefDisplayText:
                        if (m.HasValue) //注意：无加载值不写入给前端
                        {
                            writer.WritePropertyName(mm.Name);
                            writer.WriteStringValue((string)m.ObjectValue);
                        }
                        break;
                    case EntityMemberType.DataField:
                        writer.WritePropertyName(mm.Name);
                        if (m.HasValue)
                        {
                            switch (m.ValueType)
                            {
                                case EntityFieldType.Binary:
                                    writer.WriteBase64StringValue((byte[])m.ObjectValue); break;
                                case EntityFieldType.Boolean:
                                    writer.WriteBooleanValue(m.BooleanValue); break;
                                case EntityFieldType.Byte:
                                    writer.WriteNumberValue(m.ByteValue); break;
                                case EntityFieldType.DateTime:
                                    writer.WriteStringValue(m.DateTimeValue); break;
                                case EntityFieldType.Decimal:
                                    writer.WriteNumberValue(m.DecimalValue); break;
                                case EntityFieldType.Double:
                                    writer.WriteNumberValue(m.DoubleValue); break;
                                case EntityFieldType.Enum:
                                    writer.WriteNumberValue(m.Int32Value); break;
                                case EntityFieldType.Float:
                                    writer.WriteNumberValue(m.FloatValue); break;
                                case EntityFieldType.Guid:
                                    writer.WriteStringValue(m.GuidValue); break;
                                case EntityFieldType.EntityId:
                                    writer.WriteStringValue((Guid)((EntityId)m.ObjectValue)); break;
                                case EntityFieldType.Int32:
                                    writer.WriteNumberValue(m.Int32Value); break;
                                case EntityFieldType.UInt64:
                                    writer.WriteStringValue(m.UInt64Value.ToString()); break; //暂序列化为字符串
                                case EntityFieldType.String:
                                    writer.WriteStringValue((string)m.ObjectValue); break;
                                default: throw new NotSupportedException();
                            }
                        }
                        else
                        {
                            writer.WriteNullValue();
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
                    writer.Serialize(_attached[key], objrefs);
                }
            }
        }

        void IJsonSerializable.ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs)
        {
            //注意：json反序列化实体成员一律视为已变更
            var model = Model;
            EntityMemberModel memberModel = null;
            string propName = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return;

                propName = reader.GetString();
                memberModel = model.GetMember(propName, false);
                if (memberModel == null) //表示附加成员或EntitySet已删除集合
                {
                    //TODO: impl it
                    Log.Warn($"Entity json反序化读取附加成员暂未实现: {propName}");
                    reader.Skip();
                }
                if (!reader.Read())//read property value
                    throw new Exception($"Read property[{propName}] value error");

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
                            //TODO:other type
                            switch (((DataFieldModel)memberModel).DataType)
                            {
                                case EntityFieldType.Binary:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetBytes(memberModel.MemberId, null, true);
                                    else
                                        SetBytes(memberModel.MemberId, reader.GetBytesFromBase64(), true); break;
                                case EntityFieldType.Boolean:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetBooleanNullable(memberModel.MemberId, null, true);
                                    else
                                        SetBooleanNullable(memberModel.MemberId, reader.GetBoolean(), true);
                                    break;
                                case EntityFieldType.Byte:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetByteNullable(memberModel.MemberId, null, true);
                                    else
                                        SetByteNullable(memberModel.MemberId, reader.GetByte(), true);
                                    break;
                                case EntityFieldType.DateTime:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetDateTimeNullable(memberModel.MemberId, null, true);
                                    else
                                        SetDateTimeNullable(memberModel.MemberId, reader.GetDateTime(), true);
                                    break;
                                case EntityFieldType.Decimal:
                                    throw ExceptionHelper.NotImplemented();
                                case EntityFieldType.Float:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetFloatNullable(memberModel.MemberId, null, true);
                                    else
                                        SetFloatNullable(memberModel.MemberId, reader.GetSingle(), true);
                                    break;
                                case EntityFieldType.Double:
                                    throw ExceptionHelper.NotImplemented();
                                case EntityFieldType.Enum:
                                case EntityFieldType.Int32:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetInt32Nullable(memberModel.MemberId, null, true);
                                    else
                                        SetInt32Nullable(memberModel.MemberId, reader.GetInt32(), true);
                                    break;
                                case EntityFieldType.UInt64:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetUInt64Nullable(memberModel.MemberId, null, true);
                                    else
                                        SetUInt64Nullable(memberModel.MemberId, ulong.Parse(reader.GetString()), true); break;
                                case EntityFieldType.EntityId:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetEntityId(memberModel.MemberId, null, true);
                                    else
                                        SetEntityId(memberModel.MemberId, reader.GetGuid(), true);
                                    break;
                                case EntityFieldType.Guid:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetGuidNullable(memberModel.MemberId, null, true);
                                    else
                                        SetGuidNullable(memberModel.MemberId, reader.GetGuid(), true);
                                    break;
                                case EntityFieldType.String:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetString(memberModel.MemberId, null, true);
                                    else
                                        SetString(memberModel.MemberId, reader.GetString(), true); break;
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

    }
}
