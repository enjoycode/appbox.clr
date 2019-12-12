using System;
using System.Runtime.InteropServices;
using appbox.Caching;

namespace appbox.Server
{
    /// <summary>
    /// 非托管的字节缓存
    /// </summary>
    public static class ByteBuffer //TODO: rename to NativeBytesSegment
    {
        public sealed class NativeBytes : IDisposable
        {
            public IntPtr Pointer;

            private bool disposedValue; // To detect redundant calls

            public NativeBytes()
            {
                Pointer = Marshal.AllocHGlobal(BlockSize);
            }

            void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (Pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(Pointer);
                        Pointer = IntPtr.Zero;
                    }

                    disposedValue = true;
                }
            }

            ~NativeBytes()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private static readonly int BlockSize = Marshal.SizeOf(typeof(MessageChunk));
        //private static readonly ObjectPool<NativeBytes> pool = new ObjectPool<NativeBytes>(() => new NativeBytes(), 128); //TODO: check count

        public static IntPtr Pop()
        {
            //return pool.Allocate().Pointer;
            return Marshal.AllocHGlobal(BlockSize);
        }

        public static void Push(IntPtr obj)
        {
            //pool.Free(obj);
            Marshal.FreeHGlobal(obj);
        }

        public unsafe static void PushAll(MessageChunk* head)
        {
            MessageChunk* cur = head;
            MessageChunk* next = null;
            while (cur != null)
            {
                next = cur->Next;
                Push(new IntPtr(cur)); //pool.Push(new IntPtr(cur));
                cur = next;
            }
        }

        //internal unsafe static void PushAllWriteQueue(IntPtr head)
        //{
        //    MessageChunk* cur = (MessageChunk*)head.ToPointer();
        //    MessageChunk* next = null;
        //    while (cur != null)
        //    {
        //        next = cur->SendNext;
        //        pool.Push(new IntPtr(cur));
        //        cur = next;
        //    }
        //}

        //public unsafe static MessageChunk* CloneMessage(MessageChunk* source)
        //{
        //    MessageChunk* result = null;

        //    MessageChunk* cur = source;
        //    MessageChunk* next = null;
        //    while (cur != null)
        //    {
        //        next = cur->Next;

        //        //新建消息块
        //        MessageChunk* temp = (MessageChunk*)Pop().ToPointer();
        //        temp->Next = null;
        //        temp->First = result == null ? temp : result->First;

        //        //NativeMemory.Copy(new IntPtr(temp), new IntPtr(cur), MessageChunk.PayloadHeadSize + cur->DataLength); //目前只复制指定数据长度
        //        var size = MessageChunk.PayloadHeadSize + cur->DataLength;
        //        Buffer.MemoryCopy(cur, temp, size, size);

        //        if (result != null)
        //            result->Next = temp;
        //        result = temp;

        //        cur = next;
        //    }

        //    return result == null ? null : result->First;
        //}

    }

}
