using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public ulong GetUInt64(ushort mid)
        {
            return GetUInt64Nullable(mid) ?? throw new Exception("Member has no value");
        }

        public ulong? GetUInt64Nullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.UInt64)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (ulong?)m.UInt64Value : null;
        }

        public void SetUInt64(ushort mid, ulong value)
        {
            SetUInt64Nullable(mid, value);
        }

        public void SetUInt64Nullable(ushort mid, ulong? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.UInt64)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.UInt64Value || !m.HasValue)
                {
                    m.UInt64Value = value.Value;
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
