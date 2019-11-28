using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public byte GetByte(ushort mid)
        {
            return GetByteNullable(mid) ?? throw new Exception("Member has no value");
        }

        public byte? GetByteNullable(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Byte)
                throw new InvalidOperationException("Member type invalid");
            return m.Flag.HasValue ? (byte?)m.ByteValue : null;
        }

        public void SetByte(ushort mid, byte value)
        {
            SetByteNullable(mid, value);
        }

        public void SetByteNullable(ushort mid, byte? value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Byte)
                throw new InvalidOperationException("Member type invalid");
            if (value.HasValue)
            {
                if (byJsonReader || value.Value != m.ByteValue || !m.HasValue)
                {
                    m.ByteValue = value.Value;
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
