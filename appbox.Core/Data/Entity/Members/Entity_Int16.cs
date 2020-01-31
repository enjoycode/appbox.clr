using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public short GetInt16(ushort mid)
        {
            return GetInt16Nullable(mid) ?? throw new Exception("Member has no value");
        }

        public short? GetInt16Nullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Int16)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (short?)m.Int16Value : null;
        }

        public void SetInt16(ushort mid, short value)
        {
            SetInt16Nullable(mid, value);
        }

        public void SetInt16Nullable(ushort mid, short? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Int16)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.Int16Value || !m.HasValue)
                {
                    m.Int16Value = value.Value;
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
