using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using appbox.Models;
using appbox.Data;
using System.Collections.Generic;

namespace appbox.Core.Tests
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int DeptId { get; set; }
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

        [Fact]
        public void StringHashCodeTest()
        {
            var s = "Hello中";
            int hs1 = StringHelper.GetHashCode(s);
            int hs2 = s.GetHashCode();
            Assert.NotEqual(hs1, hs2);
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

        [Fact]
        public void Test4()
        {
            var s1 = "Hello Future!";
            var s2 = "Hello World!";
            var m1 = new Caching.CharsKey(s1.AsMemory(0, 5));
            var m2 = new Caching.CharsKey(s2.AsMemory(0, 5));
            Assert.True(m1.GetHashCode() == m2.GetHashCode());
        }

        [Fact]
        public void Test5()
        {
            var list = new List<Person> {
                new Person {Name = "Rick", Age= 33, DeptId =1},
                new Person {Name = "Johen", Age= 33, DeptId =1},
                new Person {Name = "Eric", Age= 43, DeptId =2},
                new Person {Name = "Steven", Age= 43, DeptId =2},
            };

            var group = list.GroupBy(t => new { t.DeptId, t.Age });
            var res = group.Select(g => new { g.Key.DeptId, g.Key.Age, SumOfAge = g.Sum(t => t.Age) });
            foreach (var item in res)
            {
                Console.WriteLine($"Key = {item.Age} SumOfAge = {item.SumOfAge}");
            }

            var q = from p in list
                    group p by p.DeptId into g
                    select new { g.Key, SumOfAge = g.Sum(t => t.Age) };
            foreach (var item in q)
            {
                
            }
        }
    }
}
