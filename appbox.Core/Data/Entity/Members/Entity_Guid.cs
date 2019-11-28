using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public Guid GetGuid(ushort mid)
        {
            return GetGuidNullable(mid) ?? throw new Exception("Member has no value");
        }

        public Guid? GetGuidNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Guid)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (Guid?)m.GuidValue : null;
        }

        public void SetGuid(ushort mid, Guid value)
        {
            SetGuidNullable(mid, value);
        }

        public void SetGuidNullable(ushort mid, Guid? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Guid)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.GuidValue || !m.HasValue)
                {
                    m.GuidValue = value.Value;
                    m.Flag.HasValue = true;
                    m.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                    OnMemberValueChanged(mid);
                }
            }
            else
            {
                ClearDataFieldValueInternal(ref m, byJsonReader);
            }
        }
    }
}
