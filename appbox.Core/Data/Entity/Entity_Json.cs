using System;
using System.Collections;
using System.IO;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;
using System.Collections.Generic;

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
                                case EntityFieldType.Int16:
                                    writer.WriteNumberValue(m.Int16Value); break;
                                case EntityFieldType.UInt16:
                                    writer.WriteNumberValue(m.UInt16Value); break;
                                case EntityFieldType.Int32:
                                    writer.WriteNumberValue(m.Int32Value); break;
                                case EntityFieldType.UInt32:
                                    writer.WriteNumberValue(m.UInt32Value); break;
                                case EntityFieldType.Int64:
                                    writer.WriteNumberValue(m.Int64Value); break;
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
            EntityMemberModel memberModel;
            string propName;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return;
                // read property name
                propName = reader.GetString();
                memberModel = model.GetMember(propName, false);
                //read property value
                if (!reader.Read())
                    throw new Exception($"Read property[{propName}] value error");

                if (memberModel == null) //表示附加成员
                {
                    //暂附加成员不可能为Object或Array
                    Log.Warn($"Read AttachedProperty[{propName}] not supported now.");
                    continue;
                }

                switch (memberModel.Type)
                {
                    case EntityMemberType.EntityRef:
                        {
                            if (reader.TokenType == JsonTokenType.Null)
                            {
                                //非新建的清空值
                                if (PersistentState != PersistentState.Detached)
                                    SetEntityRef(memberModel.MemberId, null);
                            }
                            else if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                var obj = reader.ReadObject(objrefs);
                                SetEntityRef(memberModel.MemberId, (Entity)obj);
                            }
                            else
                                throw new Exception($"Read EntityRef property[{propName}] error.");
                        }
                        break;
                    case EntityMemberType.EntitySet:
                        {
                            //注意null不用处理（不管是新建状态还是修改状态，如果修改状态前端不会传null回来）
                            if (reader.TokenType != JsonTokenType.Null)
                            {
                                //先设置为已加载状态
                                InitEntitySetForLoad((EntitySetModel)memberModel);
                                var oldList = GetEntitySet(memberModel.MemberId);
                                var newList = new List<object>();
                                //反序列化列表
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    reader.ReadList(newList, objrefs);
                                    for (int i = 0; i < newList.Count; i++)
                                    {
                                        var entity = (Entity)newList[i];
                                        //已被前端标为删除的加入已删除列表
                                        if (entity.PersistentState == PersistentState.Deleted)
                                            oldList.DeletedItems.Add(entity);
                                        else
                                            oldList.Add(entity);
                                    }
                                }
                            }
                        }
                        break;
                    case EntityMemberType.DataField:
                        {
                            //TODO:other type
                            var dfm = (DataFieldModel)memberModel;
                            switch (dfm.DataType)
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
                                        SetDateTimeNullable(memberModel.MemberId, reader.GetDateTime().ToLocalTime(), true);
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
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetDoubleNullable(memberModel.MemberId, null, true);
                                    else
                                        SetDoubleNullable(memberModel.MemberId, reader.GetDouble(), true);
                                    break;
                                case EntityFieldType.Int16:
                                    if (reader.TokenType == JsonTokenType.Null)
                                        SetInt16Nullable(memberModel.MemberId, null, true);
                                    else
                                        SetInt16Nullable(memberModel.MemberId, reader.GetInt16(), true);
                                    break;
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
                                    {
                                        //如果是新建状态的实体且为非空主键则生成Guid
                                        if (!memberModel.AllowNull && PersistentState == PersistentState.Detached
                                            && dfm.IsPrimaryKey)
                                            SetGuidNullable(memberModel.MemberId, Guid.NewGuid() /*考虑顺序Guid*/, true);
                                        else
                                            SetGuidNullable(memberModel.MemberId, null, true);
                                    }
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
                        reader.Read(); //TODO:暂忽略所有其他类型,另不要使用Skip()
                        Log.Warn($"Read member[{model.Name}.{propName}] with type[{memberModel.Type}] is not supported.");
                        break;
                }
            }

            throw new Exception("Can not read EndObjectFlag");
        }

    }
}
