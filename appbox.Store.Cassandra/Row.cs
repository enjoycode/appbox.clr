using System;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    internal struct Row : IRow
    {

        private readonly Cassandra.Row rawRow;

        public Row(Cassandra.Row rawRow)
        {
            this.rawRow = rawRow;
        }

        public Entity FetchToEntity(ulong modelId)
        {
            var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(modelId).Result;
            return FetchToEntity(model);
        }

        public Entity FetchToEntity(EntityModel model)
        {
            var members = model.Members;
            var entity = new Entity(model);
            for (int i = 0; i < members.Count; i++)
            {
                switch (members[i].Type)
                {
                    case EntityMemberType.DataField:
                        {
                            DataFieldModel dfm = (DataFieldModel)members[i];
                            if (!rawRow.IsNull(dfm.Name))
                            {
                                switch (dfm.DataType)
                                {
                                    case EntityFieldType.String:
                                        entity.SetString(dfm.MemberId, rawRow.GetValue<string>(dfm.Name)); break;
                                    case EntityFieldType.DateTime:
                                        entity.SetDateTime(dfm.MemberId,
                                            rawRow.GetValue<DateTimeOffset>(dfm.Name).LocalDateTime); break;
                                    case EntityFieldType.Int16:
                                        entity.SetInt16(dfm.MemberId, rawRow.GetValue<short>(dfm.Name)); break;
                                    case EntityFieldType.Enum:
                                    case EntityFieldType.Int32:
                                        entity.SetInt32(dfm.MemberId, rawRow.GetValue<int>(dfm.Name)); break;
                                    //case EntityFieldType.Decimal:
                                    //    entity.SetDecimal(dfm.MemberId, rawRow.GetValue<Decimal>(dfm.Name)); break;
                                    case EntityFieldType.Boolean:
                                        entity.SetBoolean(dfm.MemberId, rawRow.GetValue<bool>(dfm.Name)); break;
                                    case EntityFieldType.Guid:
                                        entity.SetGuid(dfm.MemberId, rawRow.GetValue<Guid>(dfm.Name)); break;
                                    case EntityFieldType.Byte:
                                        entity.SetByte(dfm.MemberId, unchecked((byte)rawRow.GetValue<sbyte>(dfm.Name))); break;
                                    case EntityFieldType.Binary:
                                        entity.SetBytes(dfm.MemberId, rawRow.GetValue<byte[]>(dfm.Name)); break;
                                    case EntityFieldType.Float:
                                        entity.SetFloat(dfm.MemberId, rawRow.GetValue<float>(dfm.Name)); break;
                                    case EntityFieldType.Double:
                                        entity.SetDouble(dfm.MemberId, rawRow.GetValue<double>(dfm.Name)); break;
                                    default: throw new NotSupportedException("Row.FetchToEntity.DataField");
                                }
                            }
                        }
                        break;
                    //case EntityMemberType.FieldSet:
                    //    {
                    //        FieldSetModel fsm = (FieldSetModel)members[i];
                    //        if (!rawRow.IsNull(fsm.FieldName))
                    //        {
                    //            switch (fsm.DataType)
                    //            {
                    //                case EntityFieldType.String:
                    //                    entity.SetFieldSetValue<string>(fsm.Name, new FieldSet<string>(rawRow.GetValue<IEnumerable<string>>(fsm.FieldName))); break;
                    //                case EntityFieldType.DateTime:
                    //                    entity.SetFieldSetValue<DateTime>(fsm.Name,
                    //                        new FieldSet<DateTime>(rawRow.GetValue<IEnumerable<DateTimeOffset>>(fsm.FieldName).Select(t => t.DateTime))); break;
                    //                case EntityFieldType.Enum:
                    //                case EntityFieldType.Integer:
                    //                    entity.SetFieldSetValue<int>(fsm.Name, new FieldSet<int>(rawRow.GetValue<IEnumerable<int>>(fsm.FieldName))); break;
                    //                case EntityFieldType.Decimal:
                    //                    entity.SetFieldSetValue<Decimal>(fsm.Name, new FieldSet<Decimal>(rawRow.GetValue<IEnumerable<Decimal>>(fsm.FieldName))); break;
                    //                case EntityFieldType.Guid:
                    //                    entity.SetFieldSetValue<Guid>(fsm.Name, new FieldSet<Guid>(rawRow.GetValue<IEnumerable<Guid>>(fsm.FieldName))); break;
                    //                case EntityFieldType.Byte:
                    //                    entity.SetFieldSetValue<byte>(fsm.Name, new FieldSet<byte>(rawRow.GetValue<IEnumerable<byte>>(fsm.FieldName))); break;
                    //                case EntityFieldType.Float:
                    //                    entity.SetFieldSetValue<float>(fsm.Name, new FieldSet<float>(rawRow.GetValue<IEnumerable<float>>(fsm.FieldName))); break;
                    //                case EntityFieldType.Double:
                    //                    entity.SetFieldSetValue<double>(fsm.Name, new FieldSet<double>(rawRow.GetValue<IEnumerable<double>>(fsm.FieldName))); break;
                    //                default: throw new NotSupportedException("Row.FetchToEntity.FieldSet");
                    //            }
                    //        }
                    //    }
                    //    break;
                    default: throw new NotSupportedException("Row.FetchToEntity");
                }
            }
            entity.AcceptChanges(); //todo: check need this?
            return entity;
        }

        public T Fetch<T>(Func<IRow, T> selector)
        {
            return selector(this);
        }

        public T GetValue<T>(string name) //todo: UDT处理
        {
            return rawRow.GetValue<T>(name);
        }

        public T GetValue<T>(int ordinal)
        {
            return rawRow.GetValue<T>(ordinal);
        }
    }
}
