using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal List<EntityMemberModel> Members { get; private set; } //TODO:考虑使用字典表

        public override ModelType ModelType => ModelType.Entity;

        public IEntityStoreOptions StoreOptions { get; private set; }
        internal SysStoreOptions SysStoreOptions => StoreOptions as SysStoreOptions;
        internal SqlStoreOptions SqlStoreOptions => StoreOptions as SqlStoreOptions;
        internal CqlStoreOptions CqlStoreOptions => StoreOptions as CqlStoreOptions;
        internal bool IsDTO => StoreOptions == null;

        #region ----ShortPath for Store----
#if FUTURE
        /// <summary>
        /// 存储用的标识号，模型标识号后3字节
        /// </summary>
        internal uint TableId => (uint)(Id & 0xFFFFFF);
#endif
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
        /// New EntityModel for other store
        /// </summary>
        internal EntityModel(ulong id, string name, IEntityStoreOptions storeOptions) : base(id, name)
        {
            if (storeOptions == null || storeOptions is SysStoreOptions)
                throw new ArgumentException(nameof(storeOptions));

            Members = new List<EntityMemberModel>();
            StoreOptions = storeOptions;
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
                throw new Exception($"Member not exists:{Name}.{name}");
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
                throw new Exception($"Member not exists :{Name}.{name.ToString()}");
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
#if !FUTURE
        private string _sqlTableName_cached;
#endif
        /// <summary>
        /// 用于根据规则生成Sql表的名称, eg:相同前缀、命名规则等
        /// </summary>
        /// <param name="original">true表示设计时获取旧名称</param>
        /// <param name="ctx">null表示运行时</param>
        internal string GetSqlTableName(bool original, Design.IDesignContext ctx)
        {
            Debug.Assert(SqlStoreOptions != null);
#if FUTURE
            return Name; //暂直接返回名称
#else
            if (!original && _sqlTableName_cached != null)
                return _sqlTableName_cached;

            var name = original ? OriginalName : Name;
            //TODO:根据规则生成，另注意默认存储使用默认规则
            //if ((SqlStoreOptions.DataStoreModel.NameRules & DataStoreNameRules.AppPrefixForTable)
            //    == DataStoreNameRules.AppPrefixForTable)
            //{
            ApplicationModel app = ctx == null ? Runtime.RuntimeContext.Current.GetApplicationModelAsync(AppId).Result
                : ctx.GetApplicationModel(AppId);
            if (original) return $"{app.Name}.{name}";

            _sqlTableName_cached = $"{app.Name}.{name}";
            //}
            //else
            //{
            //    _sqlTableName_cached = name;
            //}
            return _sqlTableName_cached;
#endif
        }

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

        /// <summary>
        /// 添加成员
        /// </summary>
        /// <param name="member"></param>
        /// <param name="byImport">是否导入的成员，是则不再生成标识</param>
        internal void AddMember(EntityMemberModel member, bool byImport = false)
        {
            CheckDesignMode();
            CheckOwner(member.Owner);

            if (!byImport)
            {
                //TODO:通过设计时上下文获取ApplicationModel是否导入，从而确认当前Layer
                var layer = ModelLayer.DEV;
                var seq = layer == ModelLayer.DEV ? ++_devMemberIdSeq : ++_usrMemberIdSeq;
                if (seq >= MaxMemberId)
                    throw new Exception("MemberId out of range");

                ushort memberId = (ushort)(seq << IdUtil.MEMBERID_SEQ_OFFSET | (ushort)layer << IdUtil.MEMBERID_LAYER_OFFSET);
                member.InitMemberId(memberId);
            }
            Members.Add(member);

            if (!member.AllowNull) //注意仅none nullable
                ChangeSchemaVersion();
            OnPropertyChanged();
        }

        /// <summary>
        /// 重命名成员
        /// </summary>
        internal void RenameMember(string oldName, string newName)
        {
            CheckDesignMode();
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                throw new ArgumentNullException();
            if (oldName == newName)
            {
                Log.Warn("Rename: name is same");
                return;
            }

            EntityMemberModel m = GetMember(oldName);
            m.RenameTo(newName);
            //TODO: 如果改为字典表以下需要重新加入
            //Members.Remove(oldName);
            //Members.Add(newName, m);
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

        #region ====导入方法====
        internal override void Import()
        {
            base.Import();
            foreach (var member in Members)
            {
                member.Import(this);
            }
            StoreOptions?.Import(this);
        }

        internal override bool UpdateFrom(ModelBase other)
        {
            var from = (EntityModel)other;
            bool changed = base.UpdateFrom(other);

            //导入成员，TODO:处理不同Layer的成员
            var memberComparer = new MemberComparer();
            //注意顺序:删除的 then 更新的 then 新建的
            var removedMembers = Members.Except(from.Members, memberComparer);
            foreach (var removedMember in removedMembers)
            {
                RemoveMember(removedMember.Name);
            }
            var otherMembers = Members.Intersect(from.Members, memberComparer);
            foreach (var member in otherMembers)
            {
                member.UpdateFrom(from.Members.Single(t => t.MemberId == member.MemberId));
            }
            var addedMembers = from.Members.Except(Members, memberComparer);
            foreach (var addedMember in addedMembers)
            {
                addedMember.Import(this);
                AddMember(addedMember, byImport: true);
            }

            //导入存储选项
            StoreOptions?.UpdateFrom(this, from.StoreOptions);

            //同步成员计数器
            _devMemberIdSeq = Math.Max(_devMemberIdSeq, from._devMemberIdSeq);
            //_usrMemberIdSeq = Math.Max(_usrMemberIdSeq, from._usrMemberIdSeq);

            return changed;
        }

        private class MemberComparer : IEqualityComparer<EntityMemberModel>
        {
            public bool Equals(EntityMemberModel x, EntityMemberModel y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.MemberId == y.MemberId;
            }

            public int GetHashCode(EntityMemberModel obj)
            {
                return obj == null ? 0 : obj.MemberId.GetHashCode();
            }
        }
        #endregion
    }
}
