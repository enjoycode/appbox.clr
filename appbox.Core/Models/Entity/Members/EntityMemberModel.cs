using System;
using appbox.Data;
using appbox.Serialization;
using System.Text.Json;

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
        /// <summary>
        /// 是否允许为null值
        /// </summary>
        /// <remarks>设计时改变时如果是DataField需要调用其OnDataTypeChanged</remarks>
        public bool AllowNull { get; internal set; }

        public abstract EntityMemberType Type { get; }

        internal string OriginalName => string.IsNullOrEmpty(_originalName) ? Name : _originalName;
        internal bool IsNameChanged => !string.IsNullOrEmpty(_originalName) && _originalName != Name;

        public PersistentState PersistentState { get; private set; }

        public string Comment { get; internal set; }
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
            if (!string.IsNullOrEmpty(Comment))
                bs.Write(Comment, 7);

            if (Owner.DesignMode)
            {
                bs.Write(_originalName, 5);
                bs.Write((byte)PersistentState, 6);
            }

            bs.Write(0u);
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
                    case 7: Comment = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteNumber("ID", MemberId);
            writer.WriteBoolean(nameof(AllowNull), AllowNull);
            writer.WriteString(nameof(Comment), Comment);
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(Type), (int)Type);

            WriteMembers(writer, objrefs);
        }

        protected virtual void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs) { }

        void IJsonSerializable.ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion

        #region ====导入方法====
        internal void Import(EntityModel owner)
        {
            Owner = owner ?? throw new ArgumentNullException();
            PersistentState = PersistentState.Detached;
        }

        internal virtual void UpdateFrom(EntityMemberModel from)
        {
            if (Name != from.Name)
            {
                Name = from.Name;
                //TODO:如果字典表重构，从字典表中移除旧名称，添加新名称 Owner.RenameMember()
            }
            AllowNull = from.AllowNull;
            Comment = from.Comment;
        }
        #endregion
    }

}
