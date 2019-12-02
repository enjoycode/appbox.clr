using System;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    public sealed class DataFieldModel : EntityMemberModel
    {
        #region ====Fields & Properties====
        public override EntityMemberType Type => EntityMemberType.DataField;

        public EntityFieldType DataType { get; private set; }

        /// <summary>
        /// 是否引用外键
        /// </summary>
        internal bool IsForeignKey { get; private set; }

        /// <summary>
        /// 如果DataType = Enum,则必须设置相应的EnumModel.ModelId
        /// </summary>
        internal ulong EnumModelId { get; private set; }

        /// <summary>
        /// 非空的默认值
        /// </summary>
        internal EntityMember? DefaultValue { get; private set; }

        /// <summary>
        /// 保留用于根据规则生成Sql列的名称, eg:相同前缀、命名规则等
        /// </summary>
        internal string SqlColName => Name;

        internal string SqlColOriginalName => OriginalName;

        /// <summary>
        /// 是否系统存储的分区键
        /// </summary>
        internal bool IsPartitionKey
        {
            get
            {
                if (Owner.SysStoreOptions != null && Owner.SysStoreOptions.HasPartitionKeys)
                {
                    for (int i = 0; i < Owner.SysStoreOptions.PartitionKeys.Length; i++)
                    {
                        if (Owner.SysStoreOptions.PartitionKeys[i].MemberId == MemberId)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 是否Sql存储的主键
        /// </summary>
        internal bool IsPrimaryKey
        {
            get
            {
                if (Owner.SqlStoreOptions != null && Owner.SqlStoreOptions.HasPrimaryKeys)
                {
                    for (int i = 0; i < Owner.SqlStoreOptions.PrimaryKeys.Count; i++)
                    {
                        if (Owner.SqlStoreOptions.PrimaryKeys[i].MemberId == MemberId)
                            return true;
                    }
                }
                return false;
            }
        }
        #endregion

        #region ====Ctor====
        internal DataFieldModel() { }

        internal DataFieldModel(EntityModel owner, string name,
            EntityFieldType dataType, bool isFK = false) : base(owner, name)
        {
            DataType = dataType;
            IsForeignKey = isFK;
        }
        #endregion

        #region ====Runtime Methods====
        internal override void InitMemberInstance(Entity owner, ref EntityMember member)
        {
            //TODO:处理默认值
            member.Id = MemberId;
            member.MemberType = EntityMemberType.DataField;
            member.ValueType = DataType;
            member.Flag.IsForeignKey = IsForeignKey;
            member.Flag.AllowNull = AllowNull;
            member.Flag.HasValue = !AllowNull; //必须设置
        }
        #endregion

        #region ====Design Methods====
        internal void SetDefaultValue(string value)
        {
            if (AllowNull)
                throw new NotSupportedException("Can't set default value when allow null");

            var v = new EntityMember();
            v.Id = MemberId;
            v.MemberType = EntityMemberType.DataField;
            v.ValueType = DataType;
            v.Flag.AllowNull = AllowNull;
            v.Flag.HasValue = true;

            switch (DataType)
            {
                case EntityFieldType.String:
                    v.ObjectValue = value; break;
                case EntityFieldType.DateTime:
                    v.DateTimeValue = DateTime.Parse(value); break;
                case EntityFieldType.Int32:
                    v.Int32Value = int.Parse(value); break;
                case EntityFieldType.Decimal:
                    v.DecimalValue = decimal.Parse(value); break;
                case EntityFieldType.Float:
                    v.FloatValue = float.Parse(value); break;
                case EntityFieldType.Double:
                    v.DoubleValue = double.Parse(value); break;
                case EntityFieldType.Boolean:
                    v.BooleanValue = bool.Parse(value); break;
                case EntityFieldType.Guid:
                    v.GuidValue = Guid.Parse(value); break;
                default:
                    throw new NotImplementedException();
            }

            DefaultValue = v;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write((byte)DataType, 1);
            bs.Write(IsForeignKey, 2);
            if (DataType == EntityFieldType.Enum)
                bs.Write(EnumModelId, 3);

            if (DefaultValue.HasValue)
            {
                bs.Write((uint)4);
                DefaultValue.Value.Write(bs);
            }

            bs.Write((uint)0);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: DataType = (EntityFieldType)bs.ReadByte(); break;
                    case 2: IsForeignKey = bs.ReadBoolean(); break;
                    case 3: EnumModelId = bs.ReadUInt64(); break;
                    case 4:
                        {
                            var dv = new EntityMember();
                            dv.Read(bs);
                            DefaultValue = dv; //Do not use DefaultValue.Value.Read
                            break;
                        }
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        protected override void WriteMembers(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("DataType");
            writer.WriteValue((int)DataType);

            if (DataType == EntityFieldType.Enum)
            {
                writer.WritePropertyName("EnumModelID");
                writer.WriteValue(EnumModelId);
            }
        }
        #endregion
    }
}
