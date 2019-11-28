using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace appbox.Server
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MessageChunk
    {

        //-------------传输负载消息头部分-------------
        [FieldOffset(0)]
        public byte Type; //消息类型 InvokeRequire等

        [FieldOffset(1)]
        public byte Flag; //压缩、加密、消息包结束, 另考虑消息格式标记: bin or json

        [FieldOffset(2)]
        public ushort DataLength; //数据部分长度，不包含消息头部分

        [FieldOffset(4)]
        public int ID; //消息流水号（标识），分包的消息具有相同的ID
        
        [FieldOffset(8)]
        public ulong MessageSourceID; //作为服务端消息路由的消息源ID(客户端为会话标识号，服务端为AppID) //TODO: remove, 改为消息属性

        //-------------传输负载数据部分--------------
        [FieldOffset(16)]
        private fixed byte data[PayloadDataSize];

        //-------------消息链控制部分,注意：指针统一按64位计算----------------
        [FieldOffset(232)]
        public MessageChunk* First;
        [FieldOffset(240)]
        public MessageChunk* Next;
        //-------------------------------
        [FieldOffset(248)]
        internal CircularBuffer.Node* Node; //非空表示该Chunk由AppChannel所拥有

        #region ====Statics====
        public const int PayloadHeadSize = 16;
        public const int PayloadDataSize = 216;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* GetDataPtr(MessageChunk* chunk)
        {
            return ((byte*)chunk) + PayloadHeadSize;
        }

        public static unsafe string GetDebugInfo(MessageChunk* msg, bool withData)
        {
            var sb = new System.Text.StringBuilder();
            GetDebugInfo(sb, msg, withData);
            return sb.ToString();
        }

        private static unsafe void GetDebugInfo(System.Text.StringBuilder sb, MessageChunk* msg, bool withData)
        {
            sb.AppendFormat("TYP={0} ID={1} FLAG={2} LEN={3} CUR={6} FST={4} NXT={5}"
                            , (MessageType)msg->Type, msg->ID, msg->Flag, msg->DataLength
                            , new IntPtr(msg->First), new IntPtr(msg->Next), new IntPtr(msg));
            if (withData)
            {
                sb.AppendLine();
                sb.AppendLine("------------DataStart-------------");
                sb.AppendLine(StringHelper.ToHexString(new IntPtr(GetDataPtr(msg)), msg->DataLength));
                sb.AppendLine("------------DataEnd---------------");
            }
        }

        public static unsafe string GetAllDebugInfo(MessageChunk* first, bool withData)
        {
            var sb = new System.Text.StringBuilder();
            var cur = first;
            while (cur != null)
            {
                GetDebugInfo(sb, cur, withData);
                sb.AppendLine();
                cur = cur->Next;
            }
            return sb.ToString();
        }

        public static unsafe void DumpAllData(MessageChunk* first, string file)
        {
            using (var fs = System.IO.File.OpenWrite(file))
            {
                var cur = first;
                while (cur != null)
                {
                    byte* dataPtr = GetDataPtr(cur);
                    for (int i = 0; i < cur->DataLength; i++)
                    {
                        fs.WriteByte(dataPtr[i]);
                    }
                    cur = cur->Next;
                }
            }
        }
        #endregion

    }

}
