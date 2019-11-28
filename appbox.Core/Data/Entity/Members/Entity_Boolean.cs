using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public bool GetBoolean(ushort mid)
        {
            return GetBooleanNullable(mid) ?? throw new Exception("Member has no value");
        }

        public bool? GetBooleanNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Boolean)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (bool?)m.BooleanValue : null;
        }

        public void SetBoolean(ushort mid, bool value)
        {
            SetBooleanNullable(mid, value);
        }

        public void SetBooleanNullable(ushort mid, bool? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Boolean)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.BooleanValue || !m.HasValue)
                {
                    m.BooleanValue = value.Value;
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
