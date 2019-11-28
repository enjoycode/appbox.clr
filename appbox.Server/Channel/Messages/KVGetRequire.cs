using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    public struct KVGetRequire : IMessage
    {
        public IntPtr WaitHandle { get; private set; }
        public ulong RaftGroupId { get; private set; }
        public int DataCF { get; private set; }
        public IntPtr KeyPtr { get; private set; }
        public uint KeySize { get; private set; }

        public MessageType Type => MessageType.KVGetRequire;
        public PayloadType PayloadType => PayloadType.KVGetRequire;

        public KVGetRequire(IntPtr waitHandle, ulong raftGroupid, int dataCF, IntPtr keyPtr, uint keySize)
        {
            WaitHandle = waitHandle;
            RaftGroupId = raftGroupid;
            DataCF = dataCF;
            KeyPtr = keyPtr;
            KeySize = keySize;
        }

        /// <summary>
        /// 仅用于接收方(Host进程)在调用NativeApi后释放Key
        /// </summary>
        public void FreeData()
        {
            if (KeyPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(KeyPtr);
                KeyPtr = IntPtr.Zero;
            }
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(WaitHandle.ToInt64());
            bs.Write(RaftGroupId);
            bs.Write(DataCF);
            bs.Write(KeySize);
            var span = new ReadOnlySpan<byte>(KeyPtr.ToPointer(), (int)KeySize);
            bs.Stream.Write(span);
        }

        public unsafe void ReadObject(BinSerializer bs)
        {
            WaitHandle = new IntPtr(bs.ReadInt64());
            RaftGroupId = bs.ReadUInt64();
            DataCF = bs.ReadInt32();
            KeySize = bs.ReadUInt32();
            KeyPtr = Marshal.AllocHGlobal((int)KeySize);
            var span = new Span<byte>(KeyPtr.ToPointer(), (int)KeySize);
            bs.Stream.Read(span);
        }

    }
}
