using System;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    /// <summary>
    /// KV存储的Key谓词
    /// </summary>
    public struct KeyPredicate
    {
        public readonly KeyPredicateType Type;
        internal EntityMember Value; //TODO:考虑AnyValue

        public KeyPredicate(ushort id, KeyPredicateType type, string value)
        {
            Type = type;
            Value = new EntityMember()
            {
                Id = id,
                ObjectValue = value,
                MemberType = EntityMemberType.DataField,
                ValueType = EntityFieldType.String
            };
            Value.Flag.HasValue = value != null;
        }

        public KeyPredicate(ushort id, KeyPredicateType type, int value)
        {
            Type = type;
            Value = new EntityMember()
            {
                Id = id,
                Int32Value = value,
                MemberType = EntityMemberType.DataField,
                ValueType = EntityFieldType.Int32
            };
            Value.Flag.HasValue = true;
        }

        public KeyPredicate(ushort id, KeyPredicateType type, DateTime value)
        {
            Type = type;
            Value = new EntityMember()
            {
                Id = id,
                DateTimeValue = value,
                MemberType = EntityMemberType.DataField,
                ValueType = EntityFieldType.DateTime
            };
            Value.Flag.HasValue = true;
        }
    }

    public enum KeyPredicateType : byte
    {
        Equal,
        GreaterThan,
        Between
    }
}
