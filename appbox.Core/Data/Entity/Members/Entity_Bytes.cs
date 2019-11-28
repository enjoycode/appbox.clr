using System;
using appbox.Models;

namespace appbox.Data
{
    partial class Entity
    {
        public byte[] GetBytes(ushort mid)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Binary)
                throw new InvalidOperationException("Member type invalid");
            return (byte[])m.ObjectValue;
        }

        public void SetBytes(ushort mid, byte[] value, bool byJsonReader = false)
        {
            ref EntityMember m = ref GetMember(mid);
            if (m.MemberType != EntityMemberType.DataField && m.ValueType != EntityFieldType.Binary)
                throw new InvalidOperationException("Member type invalid");

            var oldValue = (byte[])m.ObjectValue;
            if (byJsonReader || value != oldValue)
            {
                m.ObjectValue = value;
                m.Flag.HasValue = value != null;
                m.Flag.HasChanged |= PersistentState != PersistentState.Detached;
                OnMemberValueChanged(mid);
            }
        }
    }
}
