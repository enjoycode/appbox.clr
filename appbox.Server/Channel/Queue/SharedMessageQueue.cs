using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace appbox.Server
{
    /// <summary>
    /// 单向的基于共享内存的消息队列
    /// </summary>
    public sealed class SharedMessageQueue : CircularBuffer
    {
        public SharedMessageQueue(string name, int count)
            : base(name, count, Marshal.SizeOf(typeof(MessageChunk)))
        { }

        public SharedMessageQueue(string name) : base(name) { }

        #region ====Write Methods====
        /// <summary>
        /// Get MessageChunk for write.
        /// </summary>
        internal unsafe MessageChunk* GetMessageChunkForWrite()
        {
            Node* node = GetNodeForWriting(-1); //暂阻塞调用
            Debug.Assert(node != null);

            MessageChunk* chunk = (MessageChunk*)(BufferStartPtr + node->Offset);
            chunk->Node = node;
            return chunk;
        }

        /// <summary>
        /// Post the message chunk to message queue
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void PostMessageChunk(MessageChunk* chunk)
        {
            Node* node = chunk->Node;
            node->AmountWritten = MessageChunk.PayloadHeadSize + chunk->DataLength;
            chunk->Node = null;
            //Log.Debug($"发送->{MessageChunk.GetDebugInfo(chunk, false)} Written={chunk->Node->AmountWritten}");
            PostNode(node);
        }
        #endregion

        #region ====Read Methods====
        internal unsafe MessageChunk* GetMessageChunkForRead()
        {
            Node* node = GetNodeForReading(-1); //暂阻塞调用
            if (node == null)
                return null;

            MessageChunk* chunk = (MessageChunk*)(BufferStartPtr + node->Offset);
            chunk->Node = node;
            //Log.Debug($"读到数据块<-{MessageChunk.GetDebugInfo(chunk, false)}");
            return chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void ReturnMessageChunk(MessageChunk* chunk)
        {
            Debug.Assert(chunk != null);
            Node* node = chunk->Node;
            chunk->Node = null;
            ReturnNode(node);
        }
        #endregion

        #region ====Debug Methods====
        public unsafe void BuildDebugInfo(System.Text.StringBuilder sb)
        {
            NodeHeader* header = (NodeHeader*)(BufferStartPtr + NodeHeaderOffset);
            sb.AppendLine($"{Name} Nodes:{header->NodeCount} BufSize: {header->NodeBufferSize} RE:{header->ReadEnd}\tRS:{header->ReadStart}\tWE:{header->WriteEnd}\tWS:{header->WriteStart}");
        }
        #endregion
    }
}
