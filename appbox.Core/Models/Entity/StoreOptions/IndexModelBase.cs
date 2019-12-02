using System;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 系统存储及Sql存储的索引模型基类
    /// </summary>
    public abstract class IndexModelBase : IBinSerializable, IJsonSerializable
    {
        public EntityModel Owner { get; private set; }
        public byte IndexId { get; private set; }
        public string Name { get; private set; }
        public bool Unique { get; private set; }
        public FieldWithOrder[] Fields { get; private set; }
        /// <summary>
        /// 索引覆盖字段
        /// </summary>
        public ushort[] StoringFields { get; private set; }

        public bool HasStoringFields => StoringFields != null && StoringFields.Length > 0;

        public PersistentState PersistentState { get; private set; }

        #region ====Ctor====
        internal IndexModelBase() { }

        internal IndexModelBase(EntityModel owner, string name, bool unique,
            FieldWithOrder[] fields, ushort[] storingFields = null)
        {
            Owner = owner;
            Name = name;
            Unique = unique;
            Fields = fields;
            StoringFields = storingFields;
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
        public virtual void WriteObject(BinSerializer bs)
        {
            bs.Serialize(Owner, 1);
            bs.Write(IndexId, 2);
            bs.Write(Name, 3);
            bs.Write(Unique, 4);

            bs.Write((uint)6);
            bs.Write(Fields.Length);
            for (int i = 0; i < Fields.Length; i++)
            {
                Fields[i].WriteObject(bs);
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

        public virtual void ReadObject(BinSerializer bs)
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
                    case 6:
                        int count = bs.ReadInt32();
                        Fields = new FieldWithOrder[count];
                        for (int i = 0; i < count; i++)
                        {
                            Fields[i].ReadObject(bs);
                        }
                        break;
                    case 7: StoringFields = bs.ReadUInt16Array(); break;
                    case 9: PersistentState = (PersistentState)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public virtual void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("ID");
            writer.WriteValue(IndexId);

            writer.WritePropertyName(nameof(Name));
            writer.WriteValue(Name);

            writer.WritePropertyName(nameof(Unique));
            writer.WriteValue(Unique);

            writer.WritePropertyName(nameof(Fields));
            writer.WriteStartArray();
            for (int i = 0; i < Fields.Length; i++)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("MID"); //TODO: rename
                writer.WriteValue(Fields[i].MemberId);

                writer.WritePropertyName("OrderByDesc");
                writer.WriteValue(Fields[i].OrderByDesc);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion
    }
}
