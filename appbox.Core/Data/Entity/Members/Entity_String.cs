using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public string GetString(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if ((m.MemberType == EntityMemberType.DataField && m.ValueType == EntityFieldType.String)
                              /*|| m.MemberType == EntityMemberType.AutoNumber
                               || m.MemberType == EntityMemberType.EntityRefDisplayText*/)
            {
                return (string)m.ObjectValue;
            }
            //else if (m.MemberType == EntityMemberType.AggregationRefField)
            //{
            //    if (m.ObjectValue == null)
            //    {
            //        if (m.Flag.HasLoad) //尚未指向目标引用成员，并且已经加载过值
            //            return m.ObjectValue as string;

            //        m.ObjectValue = this.GetAggRefFieldTarget(name);
            //        _members[i] = m;
            //    }
            //    var target = m.ObjectValue as AggRefFieldTarget;
            //    return target.Entity.GetStringValue(target.Name);s
            //}

            throw new InvalidOperationException("Member type invalid");
        }

        public void SetString(ushort mid, string value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            //if (m.MemberType == EntityMemberType.AggregationRefField)
            //{
            //    string preLoadValue = m.ObjectValue as string;
            //    if (preLoadValue == null || preLoadValue != value)
            //    {
            //        m.ObjectValue = this.GetAggRefFieldTarget(m.Name);
            //        _members[index] = m;
            //        var target = m.ObjectValue as AggRefFieldTarget;
            //        target.Entity.SetStringValue(target.Name, value);
            //    }
            //}
            //else
            //{
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.String)
                throw new InvalidOperationException("Member type invalid");

            var oldValue = (string)m.ObjectValue;
            if (byJsonReader || value != oldValue)
            {
                m.ObjectValue = value;
                m.Flag.HasValue = value != null;
                m.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                OnMemberValueChanged(mid);
            }
            //}
        }
    }
}
