using System;
using System.Runtime.InteropServices;
using appbox.Caching;

namespace appbox.Server
{
    public static class ByteBuffer //TODO: rename to MessageBuffer or MessageChunkPool
    {

        static readonly int BlockSize = Marshal.SizeOf(typeof(MessageChunk));
        static ObjectPool<IntPtr> pool = new ObjectPool<IntPtr>(Alloc, Free, 1024);

        static IntPtr Alloc(ObjectPool<IntPtr> p)
        {
            return Marshal.AllocHGlobal(BlockSize);
        }

        static void Free(IntPtr obj)
        {
            Marshal.FreeHGlobal(obj);
        }

        public static IntPtr Pop()
        {
            return pool.Pop();
        }

        public static void Push(IntPtr obj)
        {
            pool.Push(obj);
        }

        public unsafe static void PushAll(MessageChunk* head)
        {
            MessageChunk* cur = head;
            MessageChunk* next = null;
            while (cur != null)
            {
                next = cur->Next;
                pool.Push(new IntPtr(cur));
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
