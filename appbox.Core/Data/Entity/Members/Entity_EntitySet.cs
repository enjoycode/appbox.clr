using System;
using System.Diagnostics;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {

        public EntityList GetEntitySet(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.EntitySet)
                throw new InvalidOperationException("Member type invalid");

            if (m.Flag.HasLoad)
                return (EntityList)m.ObjectValue;

            if (_persistentState == PersistentState.Detached) //是新建的实体实例，注意判断this.PersistentState会引起死循环
            {
                m.Flag.HasLoad = true;
                var list = new EntityList(this, (EntitySetModel)Model.GetMember(mid, true));
                m.ObjectValue = list;
                return list;
            }

            //暂不支持Lazy loading
            throw new NotSupportedException("Lazy loading EntitySet not supported");
        }

        /// <summary>
        /// 仅用于运行时查询初始化EntitySet成员防止重复加载
        /// </summary>
        internal void InitEntitySetForLoad(EntitySetModel entitySetMember)
        {
            ref EntityMember m = ref GetMember(entitySetMember.MemberId);
            if (m.MemberType != EntityMemberType.EntitySet)
                throw new InvalidOperationException("Member type invalid");

            if (!m.Flag.HasLoad)
            {
                m.ObjectValue = new EntityList(this, entitySetMember);
                m.Flag.HasLoad = true;
            }
        }

    }
}
