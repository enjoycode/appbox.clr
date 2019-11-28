using System;
using Xunit;
using appbox.Caching;

namespace appbox.Core.Tests
{
    public class CacheTest
    {

        [Fact]
        public void LRUCacheTest()
        {
            var key1 = new byte[] { 1, 2, 3, 4, 5 };
            var cache = new LRUCache<byte[], ulong>(100);
            ulong value = 0;
            bool getOk = cache.TryGet(key1, out value);
            Assert.False(getOk);

            cache.TryAdd(key1, 56789);
            var key2 = new byte[] { 1, 2, 3, 4, 5 };

            Console.WriteLine($"{key1.GetHashCode()},  {key2.GetHashCode()}");
            getOk = cache.TryGet(key2, out value);
            Assert.True(getOk);
            Assert.Equal<ulong>(56789, value);
        }

        [Fact]
        public unsafe void BytesKeyTest()
        {
            byte* data1 = stackalloc byte[5];
            for (int i = 0; i < 5; i++)
            {
                data1[i] = (byte)(i + 1);
            }
            var data2 = new byte[] { 1, 2, 3, 4, 5 };
            var key1 = new BytesKey(new IntPtr(data1), 5);
            var key2 = new BytesKey(data2);

            var hash1 = BytesKeyEqualityComparer.Default.GetHashCode(key1);
            var hash2 = BytesKeyEqualityComparer.Default.GetHashCode(key2);

            var cache = new LRUCache<BytesKey, int>(8, BytesKeyEqualityComparer.Default);
            cache.TryAdd(key1.CopyToManaged(), 12345);
            Assert.Equal(1, cache.Count);

            int value1 = 0;
            bool getOk1 = cache.TryGet(key1, out value1);
            Assert.True(getOk1);
            Assert.Equal(12345, value1);

            int value2 = 0;
            bool getOk2 = cache.TryGet(key2, out value2);
            Assert.True(getOk2);
            Assert.Equal(12345, value2);
        }

        [Fact]
        public unsafe void PartionKeyTest()
        {
            byte appId = 1;
            uint tableId = (uint)(Consts.SYS_EMPLOEE_MODEL_ID & 0xFFFFFF);

            int partionKeySize = 0;
            byte typeFlag = 0; //TODO:根据是否分区设置
            byte* pkPtr = stackalloc byte[5 + partionKeySize];
            pkPtr[0] = appId;
            byte* tiPtr = (byte*)&tableId;
            pkPtr[1] = tiPtr[2];
            pkPtr[2] = tiPtr[1];
            pkPtr[3] = tiPtr[0];
            pkPtr[4] = typeFlag;

            var key1 = new BytesKey(new IntPtr(pkPtr), 5 + partionKeySize);
            var cache = new LRUCache<BytesKey, ulong>(8, BytesKeyEqualityComparer.Default);
            cache.TryAdd(key1.CopyToManaged(), 12345);
            Assert.Equal(1, cache.Count);

            ulong value1 = 0;
            bool getOk1 = cache.TryGet(key1, out value1);
            Assert.True(getOk1);
            Assert.Equal<ulong>(12345, value1);
        }
    }
}
