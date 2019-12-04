using System;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 实体成员模型
    /// </summary>
    public abstract class EntityMemberModel : IBinSerializable, IJsonSerializable
    {
        #region ====Fields & Properties====
        private string _originalName;

        public EntityModel Owner { get; private set; }
        public ushort MemberId { get; private set; }
        public string Name { get; private set; }
        public bool AllowNull { get; internal set; }

        public abstract EntityMemberType Type { get; }

        internal string OriginalName => string.IsNullOrEmpty(_originalName) ? Name : _originalName;
        internal bool IsNameChanged => !string.IsNullOrEmpty(_originalName) && _originalName != Name;

        public PersistentState PersistentState { get; private set; }

        #endregion

        #region ====Ctor====
        internal EntityMemberModel() { }

        internal EntityMemberModel(EntityModel owner, string name)
        {
            Owner = owner ?? throw new ArgumentNullException();
            Name = name;
        }
        #endregion

        #region ====Runtime Methods====
        internal abstract void InitMemberInstance(Entity owner, ref EntityMember member);
        #endregion

        #region ====Design Methods====
        internal void InitMemberId(ushort id)
        {
            if (MemberId != 0)
                throw new Exception("MemberId has initialized");
            MemberId = id;
        }

        internal protected virtual void AcceptChanges()
        {
            PersistentState = PersistentState.Unchanged;
            _originalName = null;
        }

        internal void MarkDeleted()
        {
            PersistentState = PersistentState.Deleted;
            Owner.OnPropertyChanged();
        }

        internal void OnPropertyChanged()
        {
            if (PersistentState == PersistentState.Unchanged)
            {
                PersistentState = PersistentState.Modified;
                Owner.OnPropertyChanged();
            }
        }
        #endregion

        #region ====Serialization====
        public virtual void WriteObject(BinSerializer bs)
        {
            bs.Serialize(Owner, 1);
            bs.Write(AllowNull, 2);
            bs.Write(Name, 3);
            bs.Write(MemberId, 4);

            if (Owner.DesignMode)
            {
                bs.Write(_originalName, 5);
                bs.Write((byte)PersistentState, 6);
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
                    case 2: AllowNull = bs.ReadBoolean(); break;
                    case 3: Name = bs.ReadString(); break;
                    case 4: MemberId = bs.ReadUInt16(); break;
                    case 5: _originalName = bs.ReadString(); break;
                    case 6: PersistentState = (PersistentState)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + this.GetType().Name);
                }
            } while (propIndex != 0);
        }

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("ID");
            writer.WriteValue(MemberId);

            writer.WritePropertyName("AllowNull");
            writer.WriteValue(AllowNull);

            //writer.WritePropertyName("LocalizedName");
            //writer.WriteValue(this.LocalizedName);

            writer.WritePropertyName(nameof(Name));
            writer.WriteValue(Name);

            writer.WritePropertyName("Type");
            writer.WriteValue((int)Type);

            WriteMembers(writer, objrefs);
        }

        protected virtual void WriteMembers(JsonTextWriter writer, WritedObjects objrefs) { }

        void IJsonSerializable.ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion
    }

}
