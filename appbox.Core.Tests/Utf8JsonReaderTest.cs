using System;
using System.Buffers;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace appbox.Core.Tests
{
    public class Utf8JsonReaderTest
    {
        private readonly ITestOutputHelper output;
        private static readonly byte[] RequireIdPropertyName = System.Text.Encoding.UTF8.GetBytes("I");
        private static readonly byte[] RequireServicePropertyName = System.Text.Encoding.UTF8.GetBytes("S");

        public Utf8JsonReaderTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// 测试从多个Segment(utf8 bytes)中读取Json
        /// </summary>
        [Fact]
        public void ReadFromMultiSegments()
        {
            //----------------------------15----------------------------28
            var js = "{\"I\":3,\"S\":\"sys.HelloService.Hello\",\"A\":[123, \"中国\"]}";
            var data = System.Text.Encoding.UTF8.GetBytes(js);

            var bs1 = new BytesSegment(15, null); 
            data.AsSpan(0, 15).CopyTo(bs1.Buffer.AsSpan()); //sys.的.之前

            //var temp = MemoryPool<byte>.Shared.Rent(20);
            var temp1 = new byte[100];
            var bs2 = new BytesSegment(28, bs1);
            data.AsSpan(15, 28).CopyTo(bs2.Buffer.AsSpan()); //123的3之前

            var temp2 = new byte[100];
            var bs3 = new BytesSegment(data.Length - 15 - 28, bs2);
            data.AsSpan(15 + 28).CopyTo(bs3.Buffer.AsSpan());

            //var jr = new Utf8JsonReader(data.AsSpan(0, 15), false, default); //sys.的.之前
            var jr = new Utf8JsonReader(
                new ReadOnlySequence<byte>(bs1, 0, bs3, bs3.Buffer.Length), false, default);

            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.StartObject);

            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.PropertyName
                && jr.ValueSpan.SequenceEqual(RequireIdPropertyName.AsSpan()));
            Assert.True(jr.Read() && jr.GetInt32() == 3);

            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.PropertyName
                && jr.ValueSpan.SequenceEqual(RequireServicePropertyName.AsSpan()));
            Assert.True(jr.Read() && jr.GetString() == "sys.HelloService.Hello");

            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.PropertyName
                && jr.ValueTextEquals("A"));
            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.StartArray);
            Assert.True(jr.Read() && jr.GetInt32() == 123);
            Assert.True(jr.Read() && jr.GetString() == "中国");
            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.EndArray);

            Assert.True(jr.Read() && jr.TokenType == JsonTokenType.EndObject);
            Assert.True(!jr.Read());
            //Assert.Equal(jr.Read(), false); //读不完整返回false
            //var jr2 = new Utf8JsonReader(data.AsSpan(15), false, jr.CurrentState);
            //jr2.Read(); //报错误，第一个符号为.
            //Assert.Equal(service, "sys.HelloService.Hello");
        }
    }

    sealed class BytesSegment : ReadOnlySequenceSegment<byte>, IDisposable
    {
        //internal IMemoryOwner<byte> Buffer { get; }
        internal byte[] Buffer { get; }
        internal BytesSegment Previous { get; set; }
        private bool _disposed;

        public BytesSegment(int size, BytesSegment previous)
        {
            //Buffer = MemoryPool<byte>.Shared.Rent(size);
            Buffer = new byte[size];
            Previous = previous;
            if (Previous != null)
                Previous.Next = this;

            //Memory = Buffer.Memory;
            Memory = Buffer.AsMemory();
            RunningIndex = previous?.RunningIndex + previous?.Memory.Length ?? 0;
        }

        //public void SetNext(BytesSegment next) => Next = next;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                //Buffer.Dispose();
                Previous?.Dispose();
            }
        }
    }
}
