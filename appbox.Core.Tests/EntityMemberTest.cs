using System;
using Xunit;
using appbox.Data;
using appbox.Models;

namespace appbox.Core.Tests
{
    public class EntityMemberTest
    {

        [Fact]
        public void ImplicitCastTest()
        {
            EntityMember m1 = (ushort)2;
            Assert.Equal(EntityFieldType.UInt16, m1.ValueType);
            Assert.Equal(2, m1.UInt16Value);
            Assert.True(m1.HasValue);
        }

        [Fact]
        public void MemberSameTest()
        {
            EntityMember m1 = new EntityMember();
            m1.MemberType = EntityMemberType.DataField;
            m1.ValueType = EntityFieldType.String;
            m1.ObjectValue = new string('A', 5);
            m1.Flag.HasValue = true;

            EntityMember m2 = new EntityMember();
            m2.MemberType = EntityMemberType.DataField;
            m2.ValueType = EntityFieldType.String;
            m2.ObjectValue = new string('A', 5);
            m2.Flag.HasValue = true;

            Assert.True(Equals(m1.ObjectValue, m2.ObjectValue));
        }

    }
}
