#if FUTURE
using System;
using System.Diagnostics;
using appbox.Data;
using appbox.Models;
using appbox.Server;

namespace appbox.Store
{
    static class EntityStoreReader
    {
        internal static unsafe Entity ReadEntity(EntityModel model, IntPtr kp, int ks, IntPtr vp, int vs)
        {
            Debug.Assert(ks == 16);
            //Log.Debug(StringHelper.ToHexString(kv.KeyPtr, kv.KeySize.ToInt32()));
            //Log.Debug(StringHelper.ToHexString(kv.ValuePtr, kv.ValueSize.ToInt32()));

            //先处理Key,获取Id
            Guid* idPtr = (Guid*)kp;
            var obj = new Entity(model, new EntityId(*idPtr));
            ReadEntityFields(model, obj, vp, vs);
            return obj;
        }

        internal static void ReadEntityFields(EntityModel model, Entity obj, IntPtr valuePtr, int valueSize)
        {
            //TODO:暂使用KVTuple解析，待优化
            var fields = new KVTuple();
            fields.ReadFrom(valuePtr, valueSize);
            ushort fieldId;
            for (int i = 0; i < fields.fs.Count; i++)
            {
                fieldId = fields.fs[i].Id;
                var m = model.GetMember(fieldId, true);
                if (m.Type == EntityMemberType.DataField)
                {
                    var df = (DataFieldModel)m;
                    switch (df.DataType)
                    {
                        case EntityFieldType.String:
                            obj.SetString(fieldId, fields.GetString(fieldId)); break;
                        case EntityFieldType.Binary:
                            obj.SetBytes(fieldId, fields.GetBytes(fieldId)); break;
                        case EntityFieldType.Int32:
                            obj.SetInt32Nullable(fieldId, fields.GetInt32(fieldId)); break;
                        case EntityFieldType.UInt64:
                            obj.SetUInt64Nullable(fieldId, fields.GetUInt64(fieldId)); break;
                        case EntityFieldType.EntityId:
                            obj.SetEntityId(fieldId, fields.GetGuid(fieldId)); break; //TODO:可能null处理
                        case EntityFieldType.Guid:
                            obj.SetGuidNullable(fieldId, fields.GetGuid(fieldId)); break;
                        case EntityFieldType.Byte:
                            obj.SetByteNullable(fieldId, fields.GetByte(fieldId)); break;
                        case EntityFieldType.Boolean:
                            obj.SetBooleanNullable(fieldId, fields.GetBool(fieldId)); break;
                        case EntityFieldType.DateTime:
                            obj.SetDateTimeNullable(fieldId, fields.GetDateTime(fieldId)); break;
                        case EntityFieldType.Float:
                            obj.SetFloatNullable(fieldId, fields.GetFloat(fieldId)); break;
                        default:
                            throw ExceptionHelper.NotImplemented($"{df.DataType}");
                    }
                }
                else
                {
                    throw new Exception("Only read DataField");
                }
            }

            //TODO:待修改上述赋值后不再需要，参考SqlQuery填充实体
            obj.ChangeToUnChanged();//obj.AcceptChanges();
        }

    }
}
#endif