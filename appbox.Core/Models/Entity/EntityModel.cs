using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using appbox.Serialization;

namespace appbox.Models
{
    public class EntityModel : ModelBase, IJsonSerializable, IComparable<EntityModel>
    {
        private const ushort MaxMemberId = 512;

        #region ====Fields & Properties====
        private ushort _devMemberIdSeq;
        private ushort _usrMemberIdSeq;

        /// <summary>
        /// 成员列表
        /// </summary>
        /// <remarks>
        /// 注意: 仅用于内部读取，使用设计时Api修改，不要直接修改
        /// </remarks>
        internal List<EntityMemberModel> Members { get; private set; }

        public override ModelType ModelType => ModelType.Entity;

        public IEntityStoreOptions StoreOptions { get; private set; }
        internal SysStoreOptions SysStoreOptions => StoreOptions as SysStoreOptions;
        internal SqlStoreOptions SqlStoreOptions => StoreOptions as SqlStoreOptions;
        internal bool IsDTO => StoreOptions == null;

        #region ----ShortPath for Store----
        /// <summary>
        /// 存储用的标识号，模型标识号后3字节
        /// </summary>
        internal uint TableId => (uint)(Id & 0xFFFFFF);

        /// <summary>
        /// 保留用于根据规则生成Sql表的名称, eg:相同前缀、命名规则等
        /// </summary>
        internal string SqlTableName => Name;

        internal string SqlTableOriginalName => OriginalName;
        #endregion
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Only for serialization
        /// </summary>
        internal EntityModel()
        {
            Members = new List<EntityMemberModel>();
        }

        /// <summary>
        /// New EntityModel for system store
        /// </summary>
        internal EntityModel(ulong id, string name, EntityStoreType storeType, bool orderByDesc = false) : base(id, name)
        {
            Members = new List<EntityMemberModel>();
            StoreOptions = new SysStoreOptions(storeType, orderByDesc);
        }

        /// <summary>
        /// New EntityModel for sql store
        /// </summary>
        internal EntityModel(ulong id, string name, ulong storeId) : base(id, name)
        {
            Members = new List<EntityMemberModel>();
            StoreOptions = new SqlStoreOptions(storeId);
        }
        #endregion

        #region ====Member Access Methods====
        //TODO:考虑输出Member Index
        public EntityMemberModel GetMember(string name, bool throwOnNotExists)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].Name == name)
                {
                    return Members[i];
                }
            }
            if (throwOnNotExists)
                throw new Exception($"Member not exists with name:{name}");
            return null;
        }

        public EntityMemberModel GetMember(ReadOnlySpan<char> name, bool throwOnNotExists = true)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].Name.AsSpan().SequenceEqual(name))
                {
                    return Members[i];
                }
            }
            if (throwOnNotExists)
                throw new Exception($"Member not exists with name:{name.ToString()}");
            return null;
        }

        internal EntityMemberModel GetMember(ushort id, bool throwOnNotExists)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].MemberId == id)
                {
                    return Members[i];
                }
            }
            if (throwOnNotExists)
                throw new Exception($"Member not exists with id:{id}");
            return null;
        }
        #endregion

        #region ====Design Methods====
        protected internal void ChangeSchemaVersion()
        {
            if (PersistentState != Data.PersistentState.Detached && SysStoreOptions != null)
            {
                SysStoreOptions.ChangeSchemaVersion();
            }
        }

        protected internal override void AcceptChanges()
        {
            base.AcceptChanges();

            for (int i = Members.Count - 1; i >= 0; i--)
            {
                if (Members[i].PersistentState == Data.PersistentState.Deleted)
                    Members.RemoveAt(i);
                else
                    Members[i].AcceptChanges();
            }

            StoreOptions.AcceptChanges();
        }

        protected internal void CheckOwner(EntityModel owner)
        {
            if (!ReferenceEquals(owner, this))
                throw new ArgumentException("owned by other EntityModel");
        }

        protected internal void CheckDTO()
        {
            if (IsDTO)
                throw new InvalidOperationException("Not supported for DTO");
        }

        internal void AddMember(EntityMemberModel member)
        {
            CheckDesignMode();
            CheckOwner(member.Owner);

            //TODO:通过设计时上下文获取ApplicationModel是否导入，从而确认当前Layer
            var layer = ModelLayer.DEV;
            var seq = layer == ModelLayer.DEV ? ++_devMemberIdSeq : ++_usrMemberIdSeq;
            if (seq >= MaxMemberId)
                throw new Exception("MemberId out of range");

            ushort memberId = (ushort)(seq << IdUtil.MEMBERID_SEQ_OFFSET | (ushort)layer << IdUtil.MEMBERID_LAYER_OFFSET);
            member.InitMemberId(memberId);
            Members.Add(member);

            if (!member.AllowNull) //注意仅none nullable
                ChangeSchemaVersion();
            OnPropertyChanged();
        }

        /// <summary>
        /// Only used for StoreInitiator
        /// </summary>
        internal void AddSysMember(EntityMemberModel member, ushort id)
        {
            CheckDesignMode();
            CheckOwner(member.Owner);

            member.InitMemberId(id); //注意：传入id已处理Layer标记位
            Members.Add(member);
        }

        /// <summary>
        /// 根据成员名称删除成员，如果是EntityRef成员同时删除相关隐藏成员
        /// </summary>
        internal void RemoveMember(string memberName)
        {
            CheckDesignMode();
            EntityMemberModel m = GetMember(memberName, true);
            //如果实体模型是新建的或成员是新建的直接移除
            if (PersistentState == Data.PersistentState.Detached
                || m.PersistentState == Data.PersistentState.Detached)
            {
                if (m is EntityRefModel refModel)
                {
                    foreach (var fk in refModel.FKMemberIds)
                    {
                        Members.Remove(GetMember(fk, true));
                    }
                    if (refModel.IsAggregationRef)
                        Members.Remove(GetMember(refModel.TypeMemberId, true));
                }
                Members.Remove(m);
                return;
            }

            //标为删除状态
            m.MarkDeleted();
            if (m is EntityRefModel refModel2)
            {
                foreach (var fk in refModel2.FKMemberIds)
                {
                    GetMember(fk, true).MarkDeleted();
                }
                if (refModel2.IsAggregationRef)
                    GetMember(refModel2.TypeMemberId, true).MarkDeleted();
            }

            ChangeSchemaVersion();
            OnPropertyChanged();
        }
        #endregion

        #region ====Runtime Methods====
        /// <summary>
        /// 获取具有外键约束的EntityRefModel集合
        /// </summary>
        /// <returns>null没有</returns>
        internal List<EntityRefModel> GetEntityRefsWithFKConstraint()
        {
            List<EntityRefModel> list = null;
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].Type == EntityMemberType.EntityRef)
                {
                    var refModel = (EntityRefModel)Members[i];
                    if (!refModel.IsReverse && refModel.IsForeignKeyConstraint)
                    {
                        if (list == null) list = new List<EntityRefModel>();
                        list.Add(refModel);
                    }
                }
            }
            return list;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            //写入成员集
            bs.Write((uint)1);
            bs.Write(Members.Count);
            for (int i = 0; i < Members.Count; i++)
            {
                bs.Serialize(Members[i]);
            }
            //写入存储选项
            bs.Serialize(StoreOptions, 2);

            if (DesignMode)
            {
                bs.Write(_devMemberIdSeq, 3);
                bs.Write(_usrMemberIdSeq, 4);
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
                    case 1:
                        {
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var m = (EntityMemberModel)bs.Deserialize();
                                Members.Add(m);
                            }
                        }
                        break;
                    case 2: StoreOptions = (IEntityStoreOptions)bs.Deserialize(); break;
                    case 3: _devMemberIdSeq = bs.ReadUInt16(); break;
                    case 4: _usrMemberIdSeq = bs.ReadUInt16(); break;
                    //case 6: _toStringExpression = (Expression)bs.Deserialize(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex} ");
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("IsNew");
            writer.WriteBooleanValue(PersistentState == Data.PersistentState.Detached);

            //写入成员列表，注意不向前端发送EntityRef的隐藏成员及已标为删除的成员
            writer.WritePropertyName(nameof(Members));
            var q = from t in Members
                    where t.PersistentState != Data.PersistentState.Deleted
                        && !(t is DataFieldModel && ((DataFieldModel)t).IsForeignKey)
                    select t;
            writer.Serialize(q.ToArray(), objrefs);

            //写入存储选项
            if (StoreOptions != null)
            {
                writer.WritePropertyName(nameof(StoreOptions));
                writer.Serialize(StoreOptions, objrefs);
            }
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region ====IComparable====
        public int CompareTo(EntityModel other)
        {
            if (other == null)
                return 1;

            //判断当前对象有没有EntityRef引用成员至目标对象, 如果引用则大于other对象
            var refs = Members.Where(t => t.Type == EntityMemberType.EntityRef);
            foreach (var m in refs)
            {
                var rm = (EntityRefModel)m;
                foreach (var refModelId in rm.RefModelIds)
                {
                    if (refModelId == other.Id)
                    {
                        //注意：删除的需要倒过来排序
                        return other.PersistentState == Data.PersistentState.Deleted ? -1 : 1;
                    }
                }
            }

            //反过来判断,应该不需要
            var otherRefs = other.Members.Where(t => t.Type == EntityMemberType.EntityRef);
            foreach (var m in otherRefs)
            {
                var rm = (EntityRefModel)m;
                foreach (var refModelId in rm.RefModelIds)
                {
                    //注意：删除的需要倒过来排序
                    return other.PersistentState == Data.PersistentState.Deleted ? 1 : -1;
                }
            }

            return Id.CompareTo(other.Id);
        }
        #endregion
    }
}
