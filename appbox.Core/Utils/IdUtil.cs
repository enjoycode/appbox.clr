using System;
using System.Runtime.CompilerServices;
using appbox.Models;

namespace appbox
{
    internal static class IdUtil
    {
        internal const int RAFTGROUPID_APPID_OFFSET = 36;
        internal const int RAFTGROUPID_FLAGS_OFFFSET = 32;
        internal const int RAFTGROUPID_FLAGS_TYPE_OFFSET = 2;
        internal const int RAFTGROUPID_FLAGS_MVCC_OFFSET = 1;

        internal const byte RAFT_TYPE_TABLE = 0;
        internal const byte RAFT_TYPE_INDEX = 1;
        internal const byte RAFT_TYPE_BLOB_META = 2;
        //internal const byte RAFT_TYPE_BLOB_CHUNK = 3;

        internal const int MODELID_APPID_OFFSET = 32;
        internal const int MODELID_TYPE_OFFSET = 24;
        internal const int MODELID_SEQ_OFFSET = 2;

        internal const int INDEXID_UNIQUE_OFFSET = 7;

        internal const ushort MEMBERID_MASK = 0xFFE0; //2的11次方左移5位
        internal const ushort MEMBERID_LENFLAG_MASK = 0xF; //后4位
        internal const int MEMBERID_SEQ_OFFSET = 7;
        internal const int MEMBERID_LAYER_OFFSET = 5;
        internal const int MEMBERID_ORDER_OFFSET = 4;

        internal const byte STORE_FIELD_VAR_FLAG = 0;
        internal const byte STORE_FIELD_BOOL_TRUE_FLAG = 3;
        internal const byte STORE_FIELD_BOOL_FALSE_FLAG = 5;
        internal const byte STORE_FIELD_16_LEN_FLAG = 7;
        internal const byte STORE_FIELD_NULL_FLAG = 9;
        internal const ushort STORE_FIELD_ID_OF_ENTITY_ID = 7; //用于存储索引指向的实体的Id, 相当于MemberId(0) | 16LenFlag

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte GetLayerFromMemberId(ushort memberId) => (byte)((memberId >> MEMBERID_LAYER_OFFSET) & 0x3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort GetSeqFromMemberId(ushort memberId) => (ushort)(memberId >> MEMBERID_SEQ_OFFSET);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetAppIdFromModelId(ulong modelId) => (uint)(modelId >> 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ModelType GetModelTypeFromModelId(ulong modelId) => (ModelType)((modelId >> 24) & 0xFF);

    }
}
