using System;
using System.Runtime.CompilerServices;
using appbox.Caching;

namespace appbox.Server.Channel
{

    public sealed class WebSocketFrame : IBytesSegment //TODO: rename to BytesChunk
    {
        private static readonly ObjectPool<WebSocketFrame> buffers =
            new ObjectPool<WebSocketFrame>(p => new WebSocketFrame(), null, 32);

        private const int FrameSize = MessageChunk.PayloadDataSize; //注意: 等于MessageChunk的数据部分大小

        internal static WebSocketFrame Pop()
        {
            var f = buffers.Pop();
            f.First = f;
            f.Next = null;
            return f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Push(WebSocketFrame item)
        {
            item.First = item.Next = null;
            buffers.Push(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void PushAll(WebSocketFrame item)
        {
            var current = item.First;
            WebSocketFrame next;
            while(current != null)
            {   
                next = current.Next;
                Push(current);
                current = next;
            }
        }

        public byte[] Buffer { get; private set; }

        public int Length { get; internal set; }

        public WebSocketFrame Next { get; private set; }

        public WebSocketFrame First { get; private set; }

        private WebSocketFrame()
        {
            Buffer = new byte[FrameSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(WebSocketFrame next)
        {
            Next = next;
            next.First = First;
            next.Next = null;
        }

    }

}