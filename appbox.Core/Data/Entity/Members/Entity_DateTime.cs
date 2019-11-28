using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public DateTime GetDateTime(ushort mid)
        {
            return GetDateTimeNullable(mid) ?? throw new Exception("Member has no value");
        }

        public DateTime? GetDateTimeNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.DateTime)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (DateTime?)m.DateTimeValue : null;
        }

        public void SetDateTime(ushort mid, DateTime value)
        {
            SetDateTimeNullable(mid, value);
        }

        public void SetDateTimeNullable(ushort mid, DateTime? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.DateTime)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.DateTimeValue || !m.HasValue)
                {
                    m.DateTimeValue = value.Value;
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
