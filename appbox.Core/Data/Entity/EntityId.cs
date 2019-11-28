using System;
using System.Runtime.CompilerServices;
using System.Threading;
using appbox.Runtime;
using appbox.Serialization;

namespace appbox.Data
{

    //TODO:使用ObjectPool

    /// <summary>
    /// 实体实例标识号，参考doc/编码规则
    /// </summary>
    public sealed class EntityId
    {
        private static int PeerSeq; //Peer流水号计数器
        //internal static readonly EntityId Empty = new EntityId(Guid.Empty);

        internal Guid Data { get; private set; }

        public bool IsEmpty => Data == Guid.Empty;
        internal ulong RaftGroupId => GetRaftGroupId(Data);
        /// <summary>
        /// (UtcNow - UnixEpoch).TotalMilliseconds
        /// </summary>
        internal ulong Timestamp => GetTimestamp(Data);

        /// <summary>
        /// 用于隐式转换及序列化
        /// </summary>
        internal EntityId(Guid id)
        {
            Data = id;
        }

        internal EntityId()
        {
            var seq = Interlocked.Increment(ref PeerSeq) & 0xFFFF;
            ushort peerId = RuntimeContext.PeerId;
            ulong timestamp = (ulong)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
            timestamp &= 0xFFFFFFFFFFFF; //保留48bit

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
                Data = id;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void InitRaftGroupId(ulong raftGroupId)
        {
            //不需要判断是否已初始化，因为可能Schema变更后重试
            //RaftGroupId拆为32 + (12 + 4)
            //前32位
            Guid id = Data;
            WriteRaftGroupId((byte*)&id, raftGroupId);
            Data = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void WriteRaftGroupId(byte* idptr, ulong raftGroupId)
        {
            uint* p1 = (uint*)idptr;
            *p1 = (uint)(raftGroupId >> 12);
            //后12位 << 4
            ushort* p2 = (ushort*)(idptr + 4);
            *p2 = (ushort)((raftGroupId & 0xFFF) << 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ulong GetRaftGroupId(Guid id)
        {
            ulong groupId = 0;
            byte* idptr = (byte*)&id;
            uint* p1 = (uint*)idptr;
            groupId |= (ulong)*p1 << 12;
            ushort* p2 = (ushort*)(idptr + 4);
            groupId |= (ulong)*p2 >> 4;
            return groupId;
        }

        private static unsafe ulong GetTimestamp(Guid id)
        {
            ulong timestamp = 0;
            byte* idptr = (byte*)&id;
            byte* tsptr = (byte*)&timestamp;
            tsptr[0] = idptr[11];
            tsptr[1] = idptr[10];
            tsptr[2] = idptr[9];
            tsptr[3] = idptr[8];
            tsptr[4] = idptr[6];
            tsptr[5] = idptr[7];
            return timestamp;
        }

        #region ====Overrides====
        public override string ToString()
        {
            return Data.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EntityId t)) return false;
            return t.Data == Data;
        }

        public override int GetHashCode()
        {
            //去除RaftGroupId部分
            var id = Data;
            unsafe
            {
                byte* idptr = (byte*)&id;
                uint* p1 = (uint*)idptr;
                *p1 = 0;
                ushort* p2 = (ushort*)(idptr + 4);
                *p2 = 0;
            }
            return id.GetHashCode();
        }

        public static bool operator ==(EntityId lhs, EntityId rhs)
        {
            if (ReferenceEquals(lhs, rhs)) return true;
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
            return lhs.Data == rhs.Data;
        }

        public static bool operator !=(EntityId lhs, EntityId rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region ====Guid隐式转换====
        public static implicit operator Guid(EntityId id) => id.Data;

        public static implicit operator EntityId(Guid guid) => new EntityId(guid);
        #endregion
    }

}
