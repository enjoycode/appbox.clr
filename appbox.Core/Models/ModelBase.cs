using System;
using appbox.Data;
using appbox.Runtime;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 模型抽象类，注意实例状态分为设计时与运行时
    /// </summary>
    public abstract class ModelBase : IBinSerializable
    {

        #region ====Fields & Properties====
        /// <summary>
        /// 模型标识号 AppId + 流水号
        /// </summary>
        public ulong Id { get; private set; }
        public uint AppId => IdUtil.GetAppIdFromModelId(Id);
        internal ModelLayer ModleLayer => (ModelLayer)(Id & 3);

        public string Name { get; private set; }
        public bool DesignMode { get; private set; }
        public Guid? FolderId { get; internal set; }
        public PersistentState PersistentState { get; private set; }
        public uint Version { get; internal set; }

        private string _originalName;
        internal string OriginalName => string.IsNullOrEmpty(_originalName) ? Name : _originalName;
        internal bool IsNameChanged => !string.IsNullOrEmpty(_originalName) && _originalName != Name;

        public abstract ModelType ModelType { get; }
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal ModelBase() { }

        internal ModelBase(ulong id, string name)
        {
            DesignMode = true;
            Id = id;
            Name = name;
        }
        #endregion

        #region ====Methods====
        protected internal void CheckDesignMode()
        {
            if (!DesignMode)
                throw new InvalidOperationException("Not in DesignMode");
        }

        /// <summary>
        /// 发布后接受模型的所有变更
        /// </summary>
        internal protected virtual void AcceptChanges()
        {
            if (PersistentState != PersistentState.Unchanged)
            {
                if (PersistentState == PersistentState.Deleted)
                    PersistentState = PersistentState.Detached; //删除的接受变更后变为游离态
                else
                    PersistentState = PersistentState.Unchanged;

                _originalName = null;
            }
        }

        internal protected void ChangeToModified()
        {
            if (PersistentState == PersistentState.Unchanged)
                PersistentState = PersistentState.Modified;
        }

        internal protected void MarkDeleted()
        {
            if (PersistentState != PersistentState.Detached)
                PersistentState = PersistentState.Deleted;
        }
        #endregion

        #region ====IBinSerializable====
        public virtual void WriteObject(BinSerializer bs)
        {
            bs.Write(Id, 1);
            bs.Write(Name, 2);
            bs.Write(DesignMode, 3);

            if (DesignMode)
            {
                bs.Write(Version, 4);
                bs.Write((byte)PersistentState, 5);
                if (FolderId.HasValue)
                    bs.Write(FolderId.Value, 6);
                if (!string.IsNullOrEmpty(_originalName))
                    bs.Write(_originalName, 7);
            }
            else if (ModelType == ModelType.Permission)
            {
                if (FolderId.HasValue)
                    bs.Write(FolderId.Value, 6);
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
                    case 1: Id = bs.ReadUInt64(); break;
                    case 2: Name = bs.ReadString(); break;
                    case 3: DesignMode = bs.ReadBoolean(); break;
                    case 4: Version = bs.ReadUInt32(); break;
                    case 5: PersistentState = (PersistentState)bs.ReadByte(); break;
                    case 6: FolderId = bs.ReadGuid(); break;
                    case 7: _originalName = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion
    }
}
