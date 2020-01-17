using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public double GetDouble(ushort mid)
        {
            return GetDoubleNullable(mid) ?? throw new Exception("Member has no value");
        }

        public double? GetDoubleNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Double)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (double?)m.DoubleValue : null;
        }

        public void SetDouble(ushort mid, double value)
        {
            SetDoubleNullable(mid, value);
        }

        public void SetDoubleNullable(ushort mid, double? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Double)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.DoubleValue || !m.HasValue)
                {
                    m.DoubleValue = value.Value;
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
