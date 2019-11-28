using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public int GetInt32(ushort mid)
        {
            return GetInt32Nullable(mid) ?? throw new Exception("Member has no value");
        }

        public int? GetInt32Nullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Int32)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (int?)m.Int32Value : null;
        }

        public void SetInt32(ushort mid, int value)
        {
            SetInt32Nullable(mid, value);
        }

        public void SetInt32Nullable(ushort mid, int? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Int32)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.Int32Value || !m.HasValue)
                {
                    m.Int32Value = value.Value;
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
