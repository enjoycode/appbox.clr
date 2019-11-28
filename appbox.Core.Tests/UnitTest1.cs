using System;
using Xunit;
using appbox.Models;
using appbox.Data;

namespace appbox.Core.Tests
{
    class Person
    {
        public string Name { get; set; }
    }

    public class UnitTest1
    {

        private int counter = 0;

        private Person MakePerson(string name)
        {
            var v = System.Threading.Interlocked.Increment(ref counter);
            return new Person { Name = v.ToString() };
        }

        [Fact]
        public void DebuggerTest()
        {
            string name = MakePerson("Rick").Name/*调试时移至Name上每次值不同*/;
            Console.WriteLine(name);
        }

        //[Fact]
        //public void Test2()
        //{
        //    uint value = 16383; //2的14次方-1

        //    int size = 0;
        //    Serialization.VariantHelper.WriteUInt32(value, b => size++);
        //    Assert.Equal(2, size);

        //    size = 0;
        //    value = 65535;
        //    Serialization.VariantHelper.WriteUInt32(value, b => size++);
        //    Assert.Equal(3, size);
        //}

        [Fact]
        public void TestPath()
        {
            string path = "/aa/a.jpg";
            string parent = System.IO.Path.GetDirectoryName(path);
            Assert.Equal("/aa", parent);

            path = "/b.jpg";
            parent = System.IO.Path.GetDirectoryName(path);
            Assert.Equal("/", parent);
        }

        [Fact]
        public void EntityIdImplicitCastTest()
        {
            Guid idSource = Guid.NewGuid();

            var id = new EntityId(idSource);
            Guid guid = id;
            EntityId id2 = guid;
            Assert.Equal(idSource, id2.Data);
        }

        [Fact]
        public void EntityFieldTypeTest()
        {
            var field = EntityFieldType.Int32;
            var type = field.GetValueType();
            Assert.Equal("System.Int32", type.ToString());
        }

        [Fact]
        public void SpanTest()
        {
            var servicePath = "sys.OrgUnitService.SayHello";
            var span = servicePath.AsSpan();
            var firstDot = span.IndexOf('.');
            var lastDot = span.LastIndexOf('.');
            if (firstDot == lastDot)
                throw new ArgumentException(nameof(servicePath));
            var app = span.Slice(0, firstDot);
            var service = span.Slice(firstDot + 1, lastDot - firstDot - 1);
            var method = span.Slice(lastDot + 1);

            Assert.True(app.SequenceEqual("sys".AsSpan()));
            Assert.True(service.CompareTo("OrgUnitService".AsSpan(), StringComparison.Ordinal) == 0);
        }

        [Fact]
        public unsafe void Test3()
        {
            int* ptr = stackalloc int[2];
            ptr[0] = 123;
            ptr[1] = 456;

            Console.WriteLine($"Add1 = {new IntPtr(ptr)}");
            Console.WriteLine($"Add2 = {new IntPtr(ptr + 1)}");

            Assert.Equal(123, *ptr);
            Assert.Equal(456, *(ptr + 1));
        }
    }
}
