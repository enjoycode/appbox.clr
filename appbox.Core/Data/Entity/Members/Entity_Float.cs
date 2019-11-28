using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public float GetFloat(ushort mid)
        {
            return GetFloatNullable(mid) ?? throw new Exception("Member has no value");
        }

        public float? GetFloatNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Float)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (float?)m.FloatValue : null;
        }

        public void SetFloat(ushort mid, float value)
        {
            SetFloatNullable(mid, value);
        }

        public void SetFloatNullable(ushort mid, float? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Float)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.FloatValue || !m.HasValue)
                {
                    m.FloatValue = value.Value;
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
