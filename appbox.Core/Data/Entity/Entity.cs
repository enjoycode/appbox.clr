using System;
using System.Collections.Generic;
using appbox.Runtime;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Data
{
    public sealed partial class Entity : IEntityParent, IBinSerializable
    {
        #region ====Fields & Properties====
        private static EntityMember NotFound;

        /// <summary>
        /// 对应的实体模型标识
        /// </summary>
        internal ulong ModelId { get; private set; }

        public EntityId Id { get; private set; }

        internal DateTime CreateTimeUtc => DateTime.UnixEpoch.AddMilliseconds(Id.Timestamp);

        public DateTime CreateTime => CreateTimeUtc.ToLocalTime();

        private EntityMember[] _members;
        internal EntityMember[] Members => _members;

        private EntityModel _model; //仅缓存
        internal EntityModel Model
        {
            get
            {
                if (_model == null)
                    _model = RuntimeContext.Current.GetModelAsync<EntityModel>(ModelId).Result;
                return _model;
            }
        }

        private PersistentState _persistentState;
        public PersistentState PersistentState
        {
            get
            {
                //TODO:如果当前状态为UnChange且实体包含公式成员，则应判断相应的公式成员有没有改变
                return _persistentState;
            }
            internal set
            {
                _persistentState = value;
            }
        }
        #endregion

        #region ====Ctor====
        internal Entity() { }

        /// <summary>
        /// Initializes a new instance for non partitioned entity or other store
        /// </summary>
        public Entity(ulong modelId)
        {
            ModelId = modelId;
            if (Model.SysStoreOptions != null && Model.SysStoreOptions.HasPartitionKeys)
                throw new ArgumentException();
            if (Model.SysStoreOptions != null) //系统存储才需要
                Id = new EntityId();
            InitMembers(Model);
        }

        /// <summary>
        /// Initializes a new instance for partitioned entity or sql entity with pk
        /// </summary>
        public Entity(ulong modelId, params EntityMember[] pks)
        {
            ModelId = modelId;
            if (Model.SysStoreOptions != null)
            {
                if (!Model.SysStoreOptions.HasPartitionKeys
                    /*|| Model.PartitionKeys.Length != pks.Length 不能判断长度相等，因CreateTime*/)
                    throw new ArgumentException("Only ctor for partitioned entity");

                Id = new EntityId();
                InitMembers(Model);

                for (int i = 0; i < Model.SysStoreOptions.PartitionKeys.Length; i++)
                {
                    //TODO:验证Value类型是否一致
                    if (Model.SysStoreOptions.PartitionKeys[i].MemberId != 0)
                    {
                        ref EntityMember m = ref GetMember(Model.SysStoreOptions.PartitionKeys[i].MemberId);
                        m.GuidValue = pks[i].GuidValue;
                        m.ObjectValue = pks[i].ObjectValue;
                        m.Flag.HasValue = true;
                    }
                }
            }
            else if (Model.SqlStoreOptions != null)
            {
                if (!Model.SqlStoreOptions.HasPrimaryKeys
                    || Model.SqlStoreOptions.PrimaryKeys.Count != pks.Length)
                    throw new ArgumentException("Only ctor for entity with pk");

                InitMembers(Model);
                for (int i = 0; i < Model.SqlStoreOptions.PrimaryKeys.Count; i++)
                {
                    ref EntityMember m = ref GetMember(Model.SqlStoreOptions.PrimaryKeys[i].MemberId);
                    m.GuidValue = pks[i].GuidValue;
                    m.ObjectValue = pks[i].ObjectValue;
                    m.Flag.HasValue = true;
                }
            }
            else if (Model.CqlStoreOptions != null)
            {
                InitMembers(Model);
                var keys = Model.CqlStoreOptions.PrimaryKey.GetAllPKs();
                for (int i = 0; i < keys.Length; i++)
                {
                    ref EntityMember m = ref GetMember(keys[i]);
                    m.GuidValue = pks[i].GuidValue;
                    m.ObjectValue = pks[i].ObjectValue;
                    m.Flag.HasValue = true;
                }
            }
            else
            {
                throw new InvalidOperationException($"New Entity[{Model.Name}] with pks");
            }
        }

        internal Entity(EntityModel model, bool forFetch = false)
        {
            _model = model;
            ModelId = model.Id;
            if (model.SysStoreOptions != null) //系统存储才需要
                Id = new EntityId();
            InitMembers(model);
            if (forFetch)
                _persistentState = PersistentState.Unchanged;
        }

        /// <summary>
        /// Initializes a new instance for fetch from store
        /// </summary>
        internal Entity(EntityModel model, EntityId id)
        {
            _model = model;
            ModelId = model.Id;
            Id = id;
            InitMembers(model);
        }

        private void InitMembers(EntityModel model)
        {
            _members = new EntityMember[model.Members.Count];
            for (int i = 0; i < model.Members.Count; i++)
            {
                model.Members[i].InitMemberInstance(this, ref _members[i]);
            }
        }
        #endregion

        #region ====Member Access Methods====
        internal ref EntityMember GetMember(ushort memberId)
        {
            for (int i = 0; i < _members.Length; i++)
            {
                if (_members[i].Id == memberId)
                    return ref _members[i];
            }
            throw new Exception($"Member not exists with id: {memberId}");
        }

        internal ref EntityMember TryGetMember(ReadOnlySpan<char> name, out bool found)
        {
            var memberModel = Model.GetMember(name, false);
            if (memberModel == null)
            {
                found = false;
                return ref NotFound;
            }
            found = true;
            return ref GetMember(memberModel.MemberId);
        }

        internal ref EntityMember GetMember(string name)
        {
            var memberModel = Model.GetMember(name, false);
            if (memberModel == null)
            {
                throw new NotImplementedException("Get inherit member");
            }
            else
            {
                //TODO:考虑直接Index访问
                return ref GetMember(memberModel.MemberId);
            }
        }

        private void ClearDataFieldValueInternal(ref EntityMember m, bool byJsonReader)
        {
            if (m.MemberType == EntityMemberType.DataField && !m.Flag.AllowNull)
                throw new Exception("Member not AllowNull");
            m.Flag.HasChanged = byJsonReader || m.Flag.HasValue;
            m.Flag.HasLoad = m.Flag.HasValue = false;
            if (m.Flag.HasChanged)
                OnMemberValueChanged(m.Id);
        }

        private void OnMemberValueChanged(ushort id)
        {
            //TODO:排除特殊成员
            if (_persistentState == PersistentState.Unchanged)
                _persistentState = PersistentState.Modified;

            //if (PropertyChanged != null)
            //    PropertyChanged(this, new PropertyChangedEventArgs(propName));

            ////通知EntityParent实体属性发生变更
            //if (_parent != null)
            //_parent.OnChildPropertyChanged(this, propName); //todo:在这里生成Path
        }
        #endregion

        #region ====Other Methods====
        public void AcceptChanges() //TODO:考虑移除
        {
            _persistentState = PersistentState.Unchanged;
            //TODO:
        }

        /// <summary>
        /// 用于从存储加载后设为Unchanged状态
        /// </summary>
        internal void ChangeToUnChanged()
        {
            _persistentState = PersistentState.Unchanged;
        }
        #endregion

        #region ====IEntityParent Impl====
        public IEntityParent Parent { get; internal set; }

        public void OnChildPropertyChanged(Entity child, ushort childPropertyId)
        {
            //TODO: fix
            ////先找到instance对应的成员名称
            //for (int i = 0; i < this.Members.Length; i++)
            //{
            //    //再激发自身的OnPropertyChanged事件
            //    if (_members[i].MemberType == EntityMemberType.EntityRef && _members[i].ObjectValue == instance)
            //    {
            //        this.OnPropertyChanged(string.Format("{0}.{1}", _members[i].Name, propertyName));
            //        return;
            //    }
            //}

            //throw new Exception("instance is not child");
        }

        public void OnChildListChanged(EntityList childList)
        {
            //throw new NotSupportedException();
        }
        #endregion

        #region ====Serialization====
        void IBinSerializable.WriteObject(BinSerializer bs)
        {
            //TODO:写入对应的模型版本号，用于版本控制，另外判断当前会话的实体模型版本号是否是最新版本，
            //     不是则通过版本转换服务处理后再序列化
            //     需要判断是否在传输状态下，可在CompactFormatter下设置SerializedForTransmit属性

            bs.Serialize(Parent);
            //写入模型Id
            bs.Write(ModelId);
            //写入EntityId
            bs.Write(Id != null);
            if (Id != null)
                bs.Write(Id.Data);
            //写入成员
            bs.Write(Members.Length);
            for (int i = 0; i < Members.Length; i++)
            {
                _members[i].Write(bs);
            }
            //写入状态
            bs.Write((byte)_persistentState);
        }

        void IBinSerializable.ReadObject(BinSerializer bs)
        {
            //TODO:读取实例的版本号，并与最新的模型版本号比对，如果不一致，则查询有没有设置
            //     相应的版本转换服务，有则调用服务并判断是否报错。以上描述用于服务端处理
            Parent = (IEntityParent)bs.Deserialize();
            //读取模型Id
            ModelId = bs.ReadUInt64();
            //读取EntityId
            bool hasId = bs.ReadBoolean();
            if (hasId)
                Id = new EntityId(bs.ReadGuid());
            //读取成员，并重设相关属性
            int count = bs.ReadInt32();
            _members = new EntityMember[count];
            for (int i = 0; i < count; i++)
            {
                _members[i].Read(bs);
            }

            //读取状态
            _persistentState = (PersistentState)bs.ReadByte();
        }
        #endregion
    }
}
