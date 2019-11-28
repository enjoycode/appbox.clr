using System;
using Xunit;
using appbox.Data;

namespace appbox.Core.Tests
{
    public class KVTupleTest
    {
        [Fact]
        public unsafe void ReadFieldsTest()
        {
            byte[] data = StringHelper.FromHexString("40000600004141414141418000060000424242424242");
            var fields = new KVTuple();
            fixed(byte* ptr = data)
            {
                fields.ReadFrom(new IntPtr(ptr), data.Length);
                for (int i = 0; i < fields.fs.Count; i++)
                {
                    Console.WriteLine($"{fields.fs[i].Id} {fields.fs[i].DataPtr} {fields.fs[i].DataSize}");
                }
            }

            Assert.Equal("AAAAAA", fields.GetString(0x40));
            Assert.Equal("BBBBBB", fields.GetString(0x80));
        }
    }
}
