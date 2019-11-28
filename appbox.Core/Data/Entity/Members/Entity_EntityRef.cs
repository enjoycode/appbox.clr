using System;
using System.Runtime.CompilerServices;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public EntityId GetEntityId(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType == EntityMemberType.DataField && m.ValueType == EntityFieldType.EntityId)
            {
                return (EntityId)m.ObjectValue;
            }

            throw new InvalidOperationException("Member type invalid");
        }

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
                    if (refMember.IdMemberId == refKey)
                    {
                        SetEntityRefInternal(ref Members[i], null, true);
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
                    ClearDataFieldValueInternal(ref GetMember(memberModel.IdMemberId), false);
                    if (memberModel.IsAggregationRef)
                        ClearDataFieldValueInternal(ref GetMember(memberModel.TypeMemberId), false);

                    m.Flag.HasValue = false;
                    m.Flag.HasLoad = !byClear;
                    //激发已变更事件
                    //this.OnPropertyChanged(m.Name);
                }
                #endregion
            }
            else
            {
                #region ----新值不为空----
                if (!memberModel.RefModelIds.Contains(value.ModelId))
                    throw new ArgumentException($"EntityModel [{value.ModelId}] is not validated.");

                ref EntityMember idMember = ref GetMember(memberModel.IdMemberId);
                if ((!idMember.HasValue) || (idMember.HasValue && value.Id != (EntityId)idMember.ObjectValue)) //引用成员无值或值不同
                {
                    SetEntityId(memberModel.IdMemberId, value.Id);
                    if (memberModel.IsAggregationRef)
                        SetUInt64(memberModel.TypeMemberId, value.ModelId);
                    //注意:与上一语句的顺序不能交换
                    m.ObjectValue = value;
                    m.Flag.HasLoad = true;
                    //处理EntityParent
                    //注意：EntitySet内某一项对父项的EntityRef设置实例时，不应设置父项的EntityOwner为当前的EntityRef
                    //即判断当前EntityRef的Owner是否在某个EntitySet下，是则判断这个EntitySet的Owner是否就是当前设置的value
                    if (Parent == null || Parent.Parent != value)
                        value.Parent = this;

                    //this.OnPropertyChanged(m.Name); //激发已变更事件
                }
                else //值相同时 用于EntitySet的EntityList插入子对象时同步设置其父对象实例，注意：需要修改EntityList.EntitySet属性内设置过
                {
                    m.ObjectValue = value;
                    m.Flag.HasLoad = true;
                    //处理EntityParent
                    //注意：EntitySet内某一项对父项的EntityRef设置实例时，不应设置父项的EntityOwner为当前的EntityRef
                    //即判断当前EntityRef的Owner是否在某个EntitySet下，是则判断这个EntitySet的Owner是否就是当前设置的value
                    if (Parent == null || Parent.Parent != value)
                        value.Parent = this;
                }
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
