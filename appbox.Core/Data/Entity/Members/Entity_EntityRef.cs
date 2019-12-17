using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        /// <summary>
        /// 仅适用于系统存储
        /// </summary>
        public EntityId GetEntityId(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType == EntityMemberType.DataField && m.ValueType == EntityFieldType.EntityId)
            {
                return (EntityId)m.ObjectValue;
            }

            throw new InvalidOperationException("Member type invalid");
        }

        /// <summary>
        /// 仅适用于系统存储
        /// </summary>
        public void SetEntityId(ushort mid, EntityId value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.EntityId)
                throw new InvalidOperationException("Member type invalid");

            var oldValue = (EntityId)m.ObjectValue;
            if (byJsonReader || value != oldValue)
            {
                m.ObjectValue = value;
                m.Flag.HasValue = value != null;
                m.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                //清空相应的EntityRef成员的已加载值
                if (!byJsonReader)
                    ClearEntityRefValue(mid);
                OnMemberValueChanged(mid);
            }
        }

        public Entity GetEntityRef(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.EntityRef)
                throw new InvalidOperationException("Member type invalid");

            if (m.Flag.HasLoad)
                return (Entity)m.ObjectValue;
            if (_persistentState == PersistentState.Detached)
                return null;
            //暂不支持Lazy loading TODO:考虑判断外键是否有值，无值直接返回null
            throw new NotSupportedException("Lazy loading EntityRef not supported");
        }

        /// <summary>
        /// 用于外键改变值时清空已加载的EntityRef的实例
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearEntityRefValue(ushort refKey)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].MemberType == EntityMemberType.EntityRef)
                {
                    var refMember = (EntityRefModel)Model.GetMember(Members[i].Id, true);
                    for (int j = 0; j < refMember.FKMemberIds.Length; j++)
                    {
                        if (refMember.FKMemberIds[j] == refKey)
                        {
                            SetEntityRefInternal(ref Members[i], null, true);
                        }
                    }
                }
            }
        }

        public void SetEntityRef(ushort mid, Entity value)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.EntityRef)
                throw new InvalidOperationException("Member type invalid");

            SetEntityRefInternal(ref m, value, false);
        }

        private void SetEntityRefInternal(ref EntityMember m, Entity value, bool byClear)
        {
            if (value == m.ObjectValue)
                return;

            var memberModel = Model.GetMember(m.Id, true) as EntityRefModel;

            //移除旧实体的EntityParent
            if (m.ObjectValue != null)
                ((Entity)m.ObjectValue).Parent = null;

            if (value == null)
            {
                #region ----新值为空----
                //判断是否允许为空
                if (!byClear && !m.Flag.AllowNull)
                    throw new ArgumentNullException(nameof(value));

                if (m.ObjectValue != null)
                {
                    m.ObjectValue = null;
                    for (int i = 0; i < memberModel.FKMemberIds.Length; i++)
                    {
                        ClearDataFieldValueInternal(ref GetMember(memberModel.FKMemberIds[i]), false);
                    }
                    if (memberModel.IsAggregationRef)
                        ClearDataFieldValueInternal(ref GetMember(memberModel.TypeMemberId), false);

                    m.Flag.HasValue = false;
                    m.Flag.HasLoad = !byClear;
                    OnMemberValueChanged(m.Id); //激发已变更事件
                }
                #endregion
            }
            else
            {
                #region ----新值不为空----
                if (!memberModel.RefModelIds.Contains(value.ModelId))
                    throw new ArgumentException($"EntityModel [{value.ModelId}] is not validated.");

                bool fkValuesSame = true;
                //系统存储与第三方存储分别设置相关外键成员的值
                if (Model.SqlStoreOptions != null)
                {
                    var pks = value.Model.SqlStoreOptions.PrimaryKeys; //引用目标的主键
                    for (int i = 0; i < memberModel.FKMemberIds.Length; i++)
                    {
                        //当前的实体的外键成员
                        ref EntityMember fkMember = ref GetMember(memberModel.FKMemberIds[i]);
                        //设置的实体的主键成员
                        ref EntityMember pkMember = ref value.GetMember(pks[i].MemberId);
                        Debug.Assert(fkMember.Id == pkMember.Id);
                        if ((!fkMember.HasValue)
                            || !(fkMember.GuidValue == pkMember.GuidValue && fkMember.ObjectValue == pkMember.ObjectValue))
                        {
                            fkMember.GuidValue = pkMember.GuidValue;
                            fkMember.ObjectValue = pkMember.ObjectValue;
                            fkMember.Flag.HasValue = true;
                            fkMember.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                            OnMemberValueChanged(fkMember.Id);
                            fkValuesSame = false;
                        }
                    }
                }
                else
                {
                    ref EntityMember fkMember = ref GetMember(memberModel.FKMemberIds[0]);
                    if ((!fkMember.HasValue) || (value.Id != (EntityId)fkMember.ObjectValue))
                    {
                        //引用成员无值或值不同
                        //SetEntityId(memberModel.FKMemberIds[0], value.Id);
                        fkMember.ObjectValue = value.Id;
                        fkMember.Flag.HasValue = true; //value != null;
                        fkMember.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                        OnMemberValueChanged(fkMember.Id);
                        fkValuesSame = false;
                    }
                }

                //如果是聚合引用设置TypeMember成员的值
                if (memberModel.IsAggregationRef)
                    SetUInt64(memberModel.TypeMemberId, value.ModelId);
                //设置本身值，注意与上面的顺序不要交换
                m.ObjectValue = value;
                m.Flag.HasLoad = true;
                //最后处理EntityParent
                //注意：EntitySet内某一项对父项的EntityRef设置实例时，不应设置父项的EntityOwner为当前的EntityRef
                //即判断当前EntityRef的Owner是否在某个EntitySet下，是则判断这个EntitySet的Owner是否就是当前设置的value
                if (Parent == null || Parent.Parent != value)
                    value.Parent = this;
                if (!fkValuesSame)
                    OnMemberValueChanged(m.Id); //激发已变更事件
                #endregion
            }
        }

        internal void InitEntityRefForLoad(ushort refMemberId, Entity refEntity)
        {
            ref EntityMember m = ref GetMember(refMemberId);
            if (m.MemberType != EntityMemberType.EntityRef)
                throw new InvalidOperationException("Member type invalid");

            if (!m.Flag.HasLoad)
            {
                m.Flag.HasLoad = true;
            }

            if (refEntity == null) return;
            SetEntityRefInternal(ref m, refEntity, false);
        }
    }
}
