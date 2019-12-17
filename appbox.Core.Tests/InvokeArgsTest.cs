using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using appbox.Data;
using appbox.Caching;

namespace appbox.Core.Tests
{

    public class InvokeArgsTest
    {

        [Fact]
        public void ReadArgsTest1()
        {
            var js = "\"A\":[4, \"2660935927-4\", \"HelloService\"]";
            var data = System.Text.Encoding.UTF8.GetBytes(js);

            var segment = BytesSegment.Rent();
            //data.AsSpan(4).CopyTo(segment.Buffer.AsSpan());
            //segment.Length = data.Length - 4;
            //Assert.Equal(System.Text.Encoding.UTF8.GetString(segment.Memory.Span),
            //    "[4, \"2660935927-4\", \"HelloService\"]");
            data.AsSpan().CopyTo(segment.Buffer);
            segment.Length = data.Length;

            var args = InvokeArgs.From(segment, 4);
            Assert.True(args.GetInt32() == 4);
            Assert.True(args.GetString() == "2660935927-4");
            Assert.True(args.GetString() == "HelloService");
        }

        [Fact]
        public void ReadArgsTest2()
        {
            var mockRunntimeContext = new MockRuntimeContext();
            Runtime.RuntimeContext.Init(mockRunntimeContext, 10410);
            mockRunntimeContext.AddModel(TestHelper.EmploeeModel);

            var js = "\"A\":[{\"$T\":\"111428632783249997828\",\"Id\":\"01200000-0020-016e-fa64-547f10410001\",\"Name\":\"Sales Dept\"},\"01200000-0020-016e-fa64-547f10410001\"]";
            var data = System.Text.Encoding.UTF8.GetBytes(js);

            var segment = BytesSegment.Rent();
            data.AsSpan().CopyTo(segment.Buffer);
            segment.Length = data.Length;

            var args = InvokeArgs.From(segment, 4);
            var obj = args.GetObject();
            Assert.True(obj != null);
            var id = args.GetGuid();
            Assert.True(id == Guid.Parse("01200000-0020-016e-fa64-547f10410001"));
        }

    }

}
