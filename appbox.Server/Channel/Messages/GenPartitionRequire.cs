using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    public struct GenPartitionRequire : IMessage
    {
        public IntPtr TxnPtr { get; private set; }
        public IntPtr WaitHandle { get; private set; }
        internal PartitionInfo PartitionInfo { get; private set; }

        public MessageType Type => MessageType.GenPartitionRequire;
        public PayloadType PayloadType => PayloadType.GenPartitionRequire;

        public GenPartitionRequire(IntPtr txnPtr, IntPtr waitHandle, IntPtr pkPtr)
        {
            TxnPtr = txnPtr;
            WaitHandle = waitHandle;
            unsafe
            {
                PartitionInfo* src = (PartitionInfo*)pkPtr.ToPointer();
                PartitionInfo = *src;
            }
        }

        /// <summary>
        /// 仅用于接收方(Host进程)在调用NativeApi后释放Key
        /// </summary>
        public void FreeData()
        {
            if (PartitionInfo.KeyPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(PartitionInfo.KeyPtr);
            }
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(TxnPtr.ToInt64());
            bs.Write(WaitHandle.ToInt64());
            bs.Write(PartitionInfo.Flags);
            bs.Write(PartitionInfo.KeySize.ToInt32());
            var span = new ReadOnlySpan<byte>(PartitionInfo.KeyPtr.ToPointer(), PartitionInfo.KeySize.ToInt32());
            bs.Stream.Write(span);
        }

        public unsafe void ReadObject(BinSerializer bs)
        {
            TxnPtr = new IntPtr(bs.ReadInt64());
            WaitHandle = new IntPtr(bs.ReadInt64());
            var info = new PartitionInfo();
            info.Flags = bs.ReadByte();
            int size = bs.ReadInt32();
            info.KeySize = new IntPtr(size);
            info.KeyPtr = Marshal.AllocHGlobal(size);
            var span = new Span<byte>(info.KeyPtr.ToPointer(), size);
            bs.Stream.Read(span);
            PartitionInfo = info;
        }
    }
}
