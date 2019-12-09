using System;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace appbox.Caching
{

    public sealed class BytesSegment : ReadOnlySequenceSegment<byte>
    {
        #region ====Static Pool====
        private static readonly ObjectPool<BytesSegment> buffers =
                new ObjectPool<BytesSegment>(p => new BytesSegment(), null, 128);

        private const int FrameSize = 216; //MessageChunk.PayloadDataSize; //注意: 等于MessageChunk的数据部分大小

        /// <summary>
        /// 从缓存池租用一块
        /// </summary>
        internal static BytesSegment Rent()
        {
            var f = buffers.Pop();
            f.First = f;
            f.Next = null;
            return f;
        }

        /// <summary>
        /// 仅归还一块
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReturnOne(BytesSegment item)
        {
            item.First = null;
            item.Next = null;
            buffers.Push(item);
        }

        /// <summary>
        /// 归还整个链
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReturnAll(BytesSegment item)
        {
            var current = item.First;
            ReadOnlySequenceSegment<byte> next;
            while (current != null)
            {
                next = current.Next;
                ReturnOne(current);
                current = (BytesSegment)next;
            }
        }
        #endregion

        public byte[] Buffer { get; private set; }

        /// <summary>
        /// 实际数据长度，不一定等于缓存块大小
        /// </summary>
        public int Length
        {
            get { return Memory.Length; }
            internal set
            {
                if (Memory.Length != value)
                {
                    Memory = Buffer.AsMemory(0, value); //注意必须重设Memory
                }
            }
        }

        public BytesSegment First { get; private set; } //考虑移除，基类包含Pre属性

        private BytesSegment()
        {
            Buffer = new byte[FrameSize];
            Memory = Buffer.AsMemory();
            RunningIndex = 0;
        }

        /// <summary>
        /// 注意调用前必须先正确设置当前包的长度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(BytesSegment next)
        {
            Debug.Assert(Next == null);
            next.First = First;
            next.Next = null;
            next.RunningIndex = RunningIndex + Length;
            Next = next;
        }

    }

}