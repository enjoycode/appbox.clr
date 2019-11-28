using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using appbox.Models;
using appbox.Data;
using System.IO;
using appbox.Serialization;
using Serialize.Linq.Serializers;
using Serialize.Linq.Extensions;
using Xunit.Abstractions;

namespace appbox.Core.Tests
{
    public class SerializationTest
    {

        private readonly ITestOutputHelper output;

        public SerializationTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void SerializeTest()
        {
            var obj = "sys.HelloService.SayHello"; //TestHelper.SysEmploeeModel;
            byte[] data = null;
            using (var ms = new MemoryStream(1024))
            {
                BinSerializer cf = new BinSerializer(ms);
                try { cf.Serialize(obj); }
                catch (Exception) { throw; }
                finally { cf.Clear(); }

                ms.Close();
                data = ms.ToArray();
            }

            Console.WriteLine($"Data length: {data.Length}");
            Console.WriteLine(StringHelper.ToHexString(data));

            object result = null;
            using (var ms = new MemoryStream(data))
            {
                BinSerializer cf = new BinSerializer(ms);
                try { result = cf.Deserialize(); }
                catch (Exception) { throw; }
                finally { cf.Clear(); }
            }

            Console.WriteLine(result);
        }

        /// <summary>
        /// 测试C#原生ExpressionSerializer
        /// </summary>
        [Fact]
        public void ExpressionTest()
        {
            Expression<Func<int, bool>> exp1 = v => (v > 0 && v > 100) || v != 0;
            var serializer = new ExpressionSerializer(new JsonSerializer());
            var data = serializer.SerializeText(exp1);
            Console.WriteLine(data.Length);

            var exp2 = serializer.DeserializeText(data);
            Console.WriteLine("exp2:" + exp2.ToJson());
        }

        [Fact]
        public void StructArrayTest()
        {
            var pks = new PartitionKey[1];
            pks[0].MemberId = 3;
            pks[0].OrderByDesc = true;

            Assert.Equal<ushort>(3, pks[0].MemberId);
            Assert.True(pks[0].OrderByDesc);
        }

        [Fact]
        public void JSTimeTest()
        {
            string jsTime = "2019-07-28T16:00:00.000Z";
            var date = DateTime.Parse(jsTime);
            string jsArray = "[\"2019-07-28T16:00:00.000Z\"]";
            var array = Newtonsoft.Json.JsonConvert.DeserializeObject(jsArray);
            Console.WriteLine(date);
        }

        [Fact]
        public void EntityListSerializeTest()
        {
            var model = TestHelper.EmploeeModel;
            var obj = new Entity(model);
            var list = new EntityList(model.Id);
            list.Add(obj);

            byte[] data = null;
            using (var ms = new MemoryStream(1024))
            {
                BinSerializer cf = new BinSerializer(ms);
                cf.Serialize(list);
                cf.Clear();

                ms.Close();
                data = ms.ToArray();
            }

            Assert.True(data.Length > 0);
            output.WriteLine($"DataLen = {data.Length}");
        }
    }
}
