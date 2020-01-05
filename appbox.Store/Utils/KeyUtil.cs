#if FUTURE
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    static class KeyUtil
    {
        internal const ulong META_RAFTGROUP_ID = 0;

        internal const sbyte METACF_INDEX = 3;
        //internal const byte METACF_APPID_COUNTER_KEY = 0x0A;
        //internal const byte METACF_APPID_RAFTGROUPID_COUNTER_KEY = 0x0B;
        internal const byte METACF_APP_PREFIX = 0x0C;
        internal const byte METACF_MODEL_PREFIX = 0x0D;
        /// <summary>
        /// 标记MetaCF存储的索引构建状态
        /// </summary>
        internal const byte METACF_INDEX_STATE_PREFIX = 0xDD;
        internal const byte METACF_MODEL_CODE_PREFIX = 0x0E;
        internal const byte METACF_FOLDER_PREFIX = 0x0F;
        internal const byte METACF_SERVICE_ASSEMBLY_PREFIX = 0xA0;
        internal const byte METACF_VIEW_ASSEMBLY_PREFIX = 0xA1;
        internal const byte METACF_VIEW_ROUTER_PREFIX = 0xA2;

        internal const int PARTCF_INDEX = 4;
        internal const byte PARTCF_GLOBAL_TABLE_FLAG = 0x01;
        internal const byte PARTCF_PART_TABLE_FLAG = 0x02;
        internal const byte PARTCF_GLOBAL_INDEX_FLAG = 0x11;
        internal const byte PARTCF_PART_INDEX_FLAG = 0x12;

        internal const sbyte INDEXCF_INDEX = 6;
        /// <summary>
        /// IndexCF Key's IndexId所在位置
        /// </summary>
        internal const int INDEXCF_INDEXID_POS = 6;
        /// <summary>
        /// IndexCF Key前缀大小: RaftGroupId + IndexId
        /// </summary>
        internal const int INDEXCF_PREFIX_SIZE = 7;

        internal const sbyte REFINDEXCF_INDEX = 8;
        internal const int REFINDEXCF_REFFROM_KEYSIZE = 29;
        internal const int REFINDEXCF_REFTO_KEYSIZE = 35;
        private const byte REFINDEXCF_FROM_PREFIX = 0xAA;
        private const byte REFINDEXCF_TO_PREFIX = 0xBB;

        internal const int BLOBCF_INDEX = 7;
        internal const byte BLOBCF_PATH_PREFIX = 0x00;

        internal const int APP_KEY_SIZE = 5;
        internal const int MODEL_KEY_SIZE = 9;
        internal const int FOLDER_KEY_SIZE = 6;
        internal const int ENTITY_KEY_SIZE = 16;

        #region ====MetaCF====
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteAppKey(byte* keyPtr, uint appId)
        {
            byte* aidPtr = (byte*)&appId;
            keyPtr[0] = METACF_APP_PREFIX;
            for (int i = 0; i < 4; i++)
            {
                keyPtr[i + 1] = aidPtr[3 - i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteModelKey(byte* keyPtr, ulong modelId)
        {
            byte* modelIdPtr = (byte*)&modelId;
            keyPtr[0] = METACF_MODEL_PREFIX;
            for (int i = 0; i < 8; i++)
            {
                keyPtr[i + 1] = modelIdPtr[7 - i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteModelCodeKey(byte* keyPtr, ulong modelId)
        {
            byte* modelIdPtr = (byte*)&modelId;
            keyPtr[0] = METACF_MODEL_CODE_PREFIX;
            for (int i = 0; i < 8; i++)
            {
                keyPtr[i + 1] = modelIdPtr[7 - i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteFolderKey(byte* keyPtr, uint appId, ModelType modelType)
        {
            byte* aidPtr = (byte*)&appId;
            keyPtr[0] = METACF_FOLDER_PREFIX;
            for (int i = 0; i < 4; i++)
            {
                keyPtr[i + 1] = aidPtr[3 - i];
            }
            keyPtr[5] = (byte)modelType;
        }

        /// <summary>
        /// 暂服务与视图共用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteAssemblyKey(bool isService, byte* keyPtr, string asmName)
        {
            if (isService)
                keyPtr[0] = METACF_SERVICE_ASSEMBLY_PREFIX;
            else
                keyPtr[0] = METACF_VIEW_ASSEMBLY_PREFIX;

            var writer = new EntityStoreWriter(keyPtr, 1);
            writer.WriteString(asmName, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteViewRouteKey(byte* keyPtr, string viewName)
        {
            keyPtr[0] = METACF_VIEW_ROUTER_PREFIX;
            var writer = new EntityStoreWriter(keyPtr, 1);
            writer.WriteString(viewName, null);
        }
        #endregion

        #region ====TableCF====
        /// <summary>
        /// 用于写入TableCF的Key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteEntityKey(byte* keyPtr, EntityId id)
        {
            Guid* idPtr = (Guid*)(keyPtr);
            *idPtr = id.Data;
        }
        #endregion

        #region ====IndexCF====
        /// <summary>
        /// 用于写入不带谓词的索引扫描Key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteIndexKeyPrefix(byte* keyPtr, ulong raftGroupId, byte indexId)
        {
            EntityId.WriteRaftGroupId(keyPtr, raftGroupId);
            keyPtr[INDEXCF_INDEXID_POS] = indexId;
        }
        #endregion

        #region ====RefIndexCF====
        internal static unsafe void WriteRefFromKeyPrefix(byte* keyPtr, Guid id, uint tableId)
        {
            keyPtr[0] = REFINDEXCF_FROM_PREFIX;
            //Self EntityId, 写入顺序需要与WriteEntityKey一致
            Guid* idPtr = (Guid*)(keyPtr + 1);
            *idPtr = id;
            //From TableId, 入参已编码
            var fromTableIdPtr = (uint*)(keyPtr + 17);
            *fromTableIdPtr = tableId;
            //From RaftGroupId = 空
        }

        internal static unsafe void WriteRefToKeyPrefix(byte* keyPtr,
            Guid targetEntityId, ushort memberId, ulong selfRaftGroupId)
        {
            keyPtr[0] = REFINDEXCF_TO_PREFIX;
            //SelfEntityId's Part1
            EntityId.WriteRaftGroupId(keyPtr + 1, selfRaftGroupId);
            //Target EntityId
            var targetIdPtr = (Guid*)(keyPtr + 7);
            *targetIdPtr = targetEntityId;
            //RefKey MemberId
            keyPtr[23] = (byte)(memberId >> 8);
            keyPtr[24] = (byte)(memberId & 0xFF);
            //SelfEntityId's Part2不用写
        }

        /// <summary>
        /// 从RefFromKey中获取RaftGroupId, 详见编码规则
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ulong GetRaftGroupIdFromRefFromKey(IntPtr keyPtr, int keySize)
        {
            Debug.Assert(keyPtr != IntPtr.Zero);
            Debug.Assert(keySize == REFINDEXCF_REFFROM_KEYSIZE);

            var groupIdPtr = (ulong*)(keyPtr + 21).ToPointer();
            return *groupIdPtr;
        }
        #endregion

        /// <summary>
        /// 合并编码AppId + 模型TableId(大字节序)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint EncodeTableId(byte appId, uint modelTableId)
        {
            uint tableId = appId; //注意在低位，写入时反转
            unsafe
            {
                byte* ptr = (byte*)&tableId;
                byte* tiPtr = (byte*)&modelTableId;
                ptr[1] = tiPtr[2];
                ptr[2] = tiPtr[1];
                ptr[3] = tiPtr[0];
            }
            return tableId;
        }
    }
}
#endif