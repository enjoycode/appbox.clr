using System;
using System.Linq;
using appbox.Data;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Models
{
    public sealed class DataFieldModel : EntityMemberModel
    {
        #region ====Fields & Properties====
        public override EntityMemberType Type => EntityMemberType.DataField;

        /// <summary>
        /// 字段类型
        /// </summary>
        /// <remarks>set for design must call OnDataTypeChanged</remarks>
        public EntityFieldType DataType { get; internal set; }

        /// <summary>
        /// 是否引用外键
        /// </summary>
        internal bool IsForeignKey { get; private set; }

        /// <summary>
        /// 字段类型、AllowNull及DefaultValue变更均视为DataTypeChanged
        /// </summary>
        internal bool IsDataTypeChanged { get; private set; }

        /// <summary>
        /// 如果DataType = Enum,则必须设置相应的EnumModel.ModelId
        /// </summary>
        /// <remarks>set for design must call OnPropertyChanged</remarks>
        public ulong EnumModelId { get; set; }

        /// <summary>
        /// 仅用于Sql存储设置字符串最大长度(0=无限制)或Decimal整数部分长度
        /// </summary>
        /// <remarks>set for design must call OnDataTypeChanged</remarks>
        public uint Length { get; internal set; }

        /// <summary>
        /// 仅用于Sql存储设置Decimal小数部分长度
        /// </summary>
        /// <remarks>set for design must call OnDataTypeChanged</remarks>
        public uint Decimals { get; internal set; }

        /// <summary>
        /// 非空的默认值
        /// </summary>
        /// <remarks>set for design must call OnDataTypeChanged</remarks>
        internal EntityMember? DefaultValue { get; set; }

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
        /// 是否Sql或Cql存储的主键
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
                else if (Owner.CqlStoreOptions != null)
                {
                    return Owner.CqlStoreOptions.PrimaryKey.IsPrimaryKey(MemberId);
                }
                return false;
            }
        }

		public override bool AllowNull
        {
            get => base.AllowNull;
            internal set
            {
                base.AllowNull = value;
                OnDataTypeChanged();
            }
        }
		#endregion

		#region ====Ctor====

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
            member.Id = MemberId;
            member.MemberType = EntityMemberType.DataField;
            member.ValueType = DataType;
            member.Flag.IsForeignKey = IsForeignKey;
            member.Flag.AllowNull = AllowNull;
            //处理默认值
            if (DefaultValue.HasValue)
            {
                member.GuidValue = DefaultValue.Value.GuidValue;
                member.ObjectValue = DefaultValue.Value.ObjectValue;
                member.Flag.HasValue = true;
            }
            else
            {
                member.Flag.HasValue = !AllowNull; //必须设置
            }
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
            OnDataTypeChanged();
        }

        internal void OnDataTypeChanged()
        {
            if (PersistentState == PersistentState.Unchanged)
            {
                IsDataTypeChanged = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 如果当前是外键成员，则获取对应的EntityRefModel
        /// eg: OrderId成员对应的Order成员
        /// </summary>
        /// <returns>null if none fk</returns>
        internal EntityRefModel GetEntityRefModelByForeignKey()
        {
            if (!IsForeignKey) return null;
            foreach (var m in Owner.Members)
            {
                if (m is EntityRefModel rm && rm.FKMemberIds.Contains(MemberId))
                {
                    return rm;
                }
            }
            throw new Exception($"Can't find EntityRef by fk [{Owner.Name}.{Name}]");
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
            else if (DataType == EntityFieldType.String)
                bs.Write(Length, 5);
            else if (DataType == EntityFieldType.Decimal)
            {
                bs.Write(Length, 5);
                bs.Write(Decimals, 6);
            }

            if (DefaultValue.HasValue)
            {
                bs.Write((uint)4);
                DefaultValue.Value.Write(bs);
            }

            bs.Write(IsDataTypeChanged, 7);

            bs.Write(0u);
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
                    case 5: Length = bs.ReadUInt32(); break;
                    case 6: Decimals = bs.ReadUInt32(); break;
                    case 7: IsDataTypeChanged = bs.ReadBoolean(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        protected override void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteNumber(nameof(DataType), (int)DataType);

            if (DataType == EntityFieldType.Enum)
            {
                writer.WriteString(nameof(EnumModelId), EnumModelId.ToString());
            }
            else if (DataType == EntityFieldType.String)
            {
                writer.WriteNumber(nameof(Length), Length);
            }
            else if (DataType == EntityFieldType.Decimal)
            {
                writer.WriteNumber(nameof(Length), Length);
                writer.WriteNumber(nameof(Decimals), Decimals);
            }
        }
        #endregion

        #region ====导入方法====
        internal override void UpdateFrom(EntityMemberModel other)
        {
            base.UpdateFrom(other);

            var from = (DataFieldModel)other;
            //先判断是否数据类型变更, TODO:默认值是否变更
            if (DataType != from.DataType || Length != from.Length || Decimals != from.Decimals)
                OnDataTypeChanged();
            //复制属性
            DataType = from.DataType;
            IsForeignKey = from.IsForeignKey;
            Length = from.Length;
            Decimals = from.Decimals;
            EnumModelId = from.EnumModelId;
            DefaultValue = from.DefaultValue;
        }
        #endregion
    }
}
