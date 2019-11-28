using System;
using System.Collections.Generic;
using Xunit;
using appbox.Data;

namespace appbox.Core.Tests
{
    public class EntityIdTest
    {

        /// <summary>
        /// 测试EntityId时间戳最大值
        /// </summary>
        [Fact]
        public void EntityIdMaxTimeTest()
        {
            var max = 281474976710656L; //48位
            var epoch = DateTime.UnixEpoch;
            //var maxTime = epoch.AddMilliseconds(max); //直接OutOfRange
            var maxTime = DateTime.MaxValue;
            var diff = maxTime - epoch;
            Assert.True(diff.TotalMilliseconds < max);
        }

        /// <summary>
        /// 测试作为字典表的Key
        /// </summary>
        [Fact]
        public void EntityIdAsDicKeyTest()
        {
            EntityId id = new EntityId(Guid.Empty);
            var dic = new Dictionary<EntityId, int>
            {
                { id, 1 }
            };
            Assert.True(dic.ContainsKey(id));

            id.InitRaftGroupId(1234); //修改Id
            Assert.True(dic.ContainsKey(id));
        }

        [Fact]
        public unsafe void GuidOrderTest1()
        {
            Guid id1 = Guid.Parse("00000001-0002-0003-0405-060708090A0B");
            Guid id2 = Guid.Parse("20000000-0000-0000-0000-000000000000");
            byte* id1Ptr = (byte*)&id1;
            Assert.True(id1Ptr[0] == 1);
            Assert.True(id1Ptr[15] == 0x0B);
            Assert.True(id1.CompareTo(id2) < 0);
        }

        [Fact]
        public void GenEntityId()
        {
            ulong groupId = (1ul << 36) | (1ul << 34) | 1;

            var id1 = MakeEntityId(groupId, 1, out ulong ts1);
            var id2 = MakeEntityId(groupId, 2, out ulong ts2);

            Assert.Equal(groupId, EntityId.GetRaftGroupId(id1));
            Assert.Equal(groupId, EntityId.GetRaftGroupId(id2));
            Assert.Equal(ts1, id1.Timestamp);
            Assert.Equal(ts2, id2.Timestamp);
            Assert.True(id1.Data.CompareTo(id2.Data) < 0);
        }

        private unsafe static EntityId MakeEntityId(ulong groupId, int seq, out ulong ts)
        {
            ushort peerId = (1 << 12) | (1 << 6) | 1;
            ulong timestamp = (ulong)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
            timestamp &= 0xFFFFFFFFFFFF; //保留48bit
            ts = timestamp;

            unsafe
            {
                Guid id = Guid.Empty;
                byte* idptr = (byte*)&id;
                //流水号和PeerId，大字节序
                idptr[15] = (byte)(seq & 0xFF);
                idptr[14] = (byte)(seq >> 8);
                idptr[13] = (byte)(peerId & 0xFF);
                idptr[12] = (byte)(peerId >> 8);
                //时间戳部分1, 大字节序
                byte* tsptr = (byte*)&timestamp;
                idptr[11] = tsptr[0];
                idptr[10] = tsptr[1];
                idptr[9] = tsptr[2];
                idptr[8] = tsptr[3];
                //时间戳部分2, 小字节序
                idptr[7] = tsptr[5];
                idptr[6] = tsptr[4];

                var eid = new EntityId(id);
                eid.InitRaftGroupId(groupId);
                return eid;
            }
        }

    }
}
