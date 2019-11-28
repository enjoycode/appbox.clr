using System;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 仅用于上层标识索引构建状态，底层各分区状态机异步变更完后保存状态标记
    /// </summary>
    /// <remarks>
    /// 注意与存储层IndexState记录的编码一致
    /// </remarks>
    public enum EntityIndexState : byte
    {
        Ready = 0,        //索引已构建完
        Building = 1,     //索引正在构建中
        BuildFailed = 2,  //比如违反惟一性约束或超出长度限制导致异步生成失败
    }

    /// <summary>
    /// 索引字段及其排序规则
    /// </summary>
    public struct EntityIndexField
    {
        public ushort MemberId { get; private set; }
        public bool OrderByDesc { get; private set; }

        public EntityIndexField(ushort memberId, bool orderByDesc = false)
        {
            MemberId = memberId;
            OrderByDesc = orderByDesc;
        }
    }

    /// <summary>
    /// 二级索引
    /// </summary>
    public sealed class EntityIndexModel : IBinSerializable, IJsonSerializable
    {
        public EntityModel Owner { get; private set; }
        public byte IndexId { get; private set; }
        public string Name { get; private set; }
        public bool Unique { get; private set; }
        /// <summary>
        /// 是否全局索引,不支持非Mvcc表全局索引无法保证一致性
        /// </summary>
        public bool Global { get; private set; }
        public EntityIndexState State { get; internal set; } 
        public EntityIndexField[] Fields { get; private set; }
        /// <summary>
        /// 索引覆盖字段
        /// </summary>
        public ushort[] StoringFields { get; private set; }

        public bool HasStoringFields => StoringFields != null && StoringFields.Length > 0;

        public PersistentState PersistentState { get; private set; }

        //TODO: property IncludeNull

        #region ====Ctor====
        internal EntityIndexModel() { }

        internal EntityIndexModel(EntityModel owner, string name, bool unique,
            EntityIndexField[] fields, ushort[] storingFields = null)
        {
            Owner = owner;
            Name = name;
            Unique = unique;
            Fields = fields;
            StoringFields = storingFields;
            State = owner.PersistentState == PersistentState.Detached ? EntityIndexState.Ready : EntityIndexState.Building;
        }
        #endregion

        #region ====Design Methods====
        internal void InitIndexId(byte id)
        {
            if (IndexId != 0)
                throw new Exception("IndexId has initialized");
            IndexId = id;
        }

        internal void AcceptChanges()
        {
            PersistentState = PersistentState.Unchanged;
        }

        internal void MarkDeleted()
        {
            PersistentState = PersistentState.Deleted;
            Owner.ChangeToModified();
            Owner.ChangeSchemaVersion();
        }
        #endregion

        #region ====Serialization====
        void IBinSerializable.WriteObject(BinSerializer bs)
        {
            bs.Serialize(Owner, 1);
            bs.Write(IndexId, 2);
            bs.Write(Name, 3);
            bs.Write(Unique, 4);
            bs.Write(Global, 8);
            bs.Write((byte)State, 5);

            bs.Write((uint)6);
            bs.Write(Fields.Length);
            for (int i = 0; i < Fields.Length; i++)
            {
                bs.Write(Fields[i].MemberId);
                bs.Write(Fields[i].OrderByDesc);
            }

            if (StoringFields != null && StoringFields.Length > 0)
            {
                bs.Write(StoringFields, 7);
            }

            if (Owner.DesignMode)
            {
                bs.Write((byte)PersistentState, 9);
            }

            bs.Write((uint)0);
        }

        void IBinSerializable.ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Owner = (EntityModel)bs.Deserialize(); break;
                    case 2: IndexId = bs.ReadByte(); break;
                    case 3: Name = bs.ReadString(); break;
                    case 4: Unique = bs.ReadBoolean(); break;
                    case 5: State = (EntityIndexState)bs.ReadByte(); break;
                    case 6:
                        int count = bs.ReadInt32();
                        Fields = new EntityIndexField[count];
                        for (int i = 0; i < count; i++)
                        {
                            var mid = bs.ReadUInt16();
                            var order = bs.ReadBoolean();
                            Fields[i] = new EntityIndexField(mid, order);
                        }
                        break;
                    case 7: StoringFields = bs.ReadUInt16Array(); break;
                    case 8: Global = bs.ReadBoolean(); break;
                    case 9: PersistentState = (PersistentState)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("ID");
            writer.WriteValue(IndexId);

            writer.WritePropertyName(nameof(Name));
            writer.WriteValue(Name);

            writer.WritePropertyName(nameof(Unique));
            writer.WriteValue(Unique);

            writer.WritePropertyName(nameof(Global));
            writer.WriteValue(Global);

            writer.WritePropertyName(nameof(State));
            writer.WriteValue(State.ToString());

            writer.WritePropertyName(nameof(Fields));
            writer.WriteStartArray();
            for (int i = 0; i < Fields.Length; i++)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("MID");
                writer.WriteValue(Fields[i].MemberId);

                writer.WritePropertyName("OrderByDesc");
                writer.WriteValue(Fields[i].OrderByDesc);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
