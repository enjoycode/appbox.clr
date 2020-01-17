#if FUTURE
using System;
using Xunit;
using appbox.Data;
using appbox.Store;

namespace appbox.Design.Tests
{
    public class KeyUtilsTest
    {
        [Fact]
        public void EncodeTableId()
        {
            byte appStoreId = 1;
            uint modelTableId = 0xAABBCC;

            uint encodedTableId = KeyUtil.EncodeTableId(appStoreId, modelTableId);
            Assert.True(encodedTableId == 0xCCBBAA01);
        }

        [Fact]
        public unsafe void EntityKeyTest()
        {
            //00002001-3000-15D78851776115104102
            ulong flags1 = 1 << 1; //RaftType | MvccFlag | OrderFlag
            ulong raftGroupId1 = (1ul << 36) | (flags1 << 32 | 3); //AppId << 36 | flags << 32 | counter

            EntityId id = Guid.Empty;
            id.InitRaftGroupId(raftGroupId1);

            byte* keyPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
            KeyUtil.WriteEntityKey(keyPtr, id);
            var hex = StringHelper.ToHexString(new IntPtr(keyPtr), KeyUtil.ENTITY_KEY_SIZE);
            Assert.Equal("00002001300000000000000000000000", hex);

            ulong flags2 = 1 << 1 | 1;
            ulong raftGroupId2 = (1ul << 36) | (flags2 << 32 | 3);
            id = Guid.Empty; //new EntityId();
            id.InitRaftGroupId(raftGroupId2);
            KeyUtil.WriteEntityKey(keyPtr, id);
            hex = StringHelper.ToHexString(new IntPtr(keyPtr), KeyUtil.ENTITY_KEY_SIZE);
            Console.WriteLine(hex);
        }

        [Fact]
        public unsafe void EntityIdOrderTest()
        {
            var key1 = StringHelper.FromHexString("00002001300000000000000000000000");
            var key2 = StringHelper.FromHexString("00003001300000000000000000000000");
            fixed (byte* key1Ptr = key1)
            fixed (byte* key2Ptr = key2)
            {
                uint* p1 = (uint*)key1Ptr;
                uint* p2 = (uint*)key2Ptr;

                var appId1 = *p1 >> (20 + 1 + 1 + 2);
                var appId2 = *p2 >> (20 + 1 + 1 + 2);
                Assert.Equal<uint>(1, appId1);
                Assert.Equal<uint>(1, appId2);

                var typeFlag1 = (*p1 >> (20 + 1 + 1)) & 1;
                var typeFlag2 = (*p2 >> (20 + 1 + 1)) & 1;
                Assert.Equal<uint>(0, typeFlag1);
                Assert.Equal<uint>(0, typeFlag2);

                var mvccFlag1 = (*p1 >> (20 + 1)) & 1;
                var mvccFlag2 = (*p2 >> (20 + 1)) & 1;
                Assert.Equal<uint>(1, mvccFlag1);
                Assert.Equal<uint>(1, mvccFlag2);

                var orderFlag1 = (*p1 >> 20) & 1;
                var orderFlag2 = (*p2 >> 20) & 1;
                Assert.Equal<uint>(0, orderFlag1);
                Assert.Equal<uint>(1, orderFlag2);
            }
        }
    }
}
#endif