using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 模型的文件夹，每个应用的模型类型对应一个根文件夹
    /// 修改时签出对应的模型根节点
    /// </summary>
    public sealed class ModelFolder : IBinSerializable
    {

        #region ====Fields & Properties====
        /// <summary>
        /// 根文件夹为null
        /// </summary>
        public string Name { get; set; }

        public ModelFolder Parent { get; internal set; }

        public uint AppId { get; private set; }

        public ModelType TargetModelType { get; private set; }

        /// <summary>
        /// 根文件夹为Guid.Empty
        /// </summary>
        public Guid Id { get; private set; }

        public int SortNum { get; set; }

        public uint Version { get; internal set; } //TODO: remove?

        private List<ModelFolder> _childs;

        public List<ModelFolder> Childs
        {
            get
            {
                if (_childs == null)
                    _childs = new List<ModelFolder>();
                return _childs;
            }
        }

        public bool HasChilds => _childs != null && _childs.Count > 0;

        /// <summary>
        /// 仅Root有效
        /// </summary>
        public bool IsDeleted { get; internal set; }
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal ModelFolder() { }

        /// <summary>
        /// Create root folder
        /// </summary>
        internal ModelFolder(uint appID, ModelType targetModelType)
        {
            //RootFolder Id = Guid.Empty & Name = null
            AppId = appID;
            TargetModelType = targetModelType;
        }

        /// <summary>
        /// Create child folder
        /// </summary>
        internal ModelFolder(ModelFolder parent, string name)
        {
            Id = Guid.NewGuid();
            AppId = parent.AppId;
            Parent = parent;
            Name = name;
            TargetModelType = parent.TargetModelType;
            Parent.Childs.Add(this);
        }
        #endregion

        #region ====Designtime Methods====
        /// <summary>
        /// 移除文件夹
        /// </summary>
        public void Remove()
        {
            if (Parent == null)
                throw new InvalidOperationException("Can't remove root folder");
            Parent.Childs.Remove(this);
        }

        public ModelFolder GetRoot()
        {
            if (Parent != null)
                return Parent.GetRoot();
            return this;
        }
        #endregion

        #region ====IBinSerializable Implements====
        public void WriteObject(BinSerializer cf)
        {
            if (Parent != null)
            {
                cf.Write(Id, 1);
                cf.Write(Name, 2);
                cf.Serialize(Parent, 4);
                if (TargetModelType == ModelType.Permission) //仅权限文件夹排序
                    cf.Write(SortNum, 8);
            }
            else
            {
                cf.Write(Version, 3);
                cf.Write(IsDeleted, 9);
            }
            if (HasChilds)
                cf.WriteList(_childs, 5);
            cf.Write(AppId, 6);
            cf.Write((byte)TargetModelType, 7);

            cf.Write(0u);
        }

        public void ReadObject(BinSerializer cf)
        {
            uint propIndex;
            do
            {
                propIndex = cf.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Id = cf.ReadGuid(); break;
                    case 2: Name = cf.ReadString(); break;
                    case 3: Version = cf.ReadUInt32(); break;
                    case 4: Parent = (ModelFolder)cf.Deserialize(); break;
                    case 5: _childs = cf.ReadList<ModelFolder>(); break;
                    case 6: AppId = cf.ReadUInt32(); break;
                    case 7: TargetModelType = (ModelType)cf.ReadByte(); break;
                    case 8: SortNum = cf.ReadInt32(); break;
                    case 9: IsDeleted = cf.ReadBoolean(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }
        #endregion

        #region ====导入方法====
        public void Import()
        {
            Version -= 1; //注意：-1，发布时+1
        }

        public bool UpdateFrom(ModelFolder from)
        {
            Version = from.Version - 1; //注意：-1，发布时+1
            //TODO:暂简单同步处理
            Name = from.Name;
            _childs = from._childs; //直接复制
            return true;
        }
        #endregion
    }
}
