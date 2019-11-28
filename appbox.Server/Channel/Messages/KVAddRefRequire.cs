using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// 用于子进程包装ClrAddRefRequire传给主进程后调用NativeApi
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KVAddRefRequire : IMessage
    {
        public ClrAddRefRequire Require { get; private set; } //注意不要调整顺序，与native一致
        public IntPtr WaitHandle { get; private set; }
        public IntPtr TxnPtr { get; private set; }

        public MessageType Type => MessageType.KVAddRefRequire;
        public PayloadType PayloadType => PayloadType.KVAddRefRequire;

        public KVAddRefRequire(IntPtr waitHandle, IntPtr txnPtr, IntPtr reqPtr)
        {
            WaitHandle = waitHandle;
            TxnPtr = txnPtr;
            unsafe
            {
                ClrAddRefRequire* req = (ClrAddRefRequire*)reqPtr.ToPointer();
                Require = *req;
            }
        }

        internal void FreeData()
        {
            if (Require.KeyPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.KeyPtr);
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(WaitHandle.ToInt64());
            bs.Write(TxnPtr.ToInt64());
            bs.Write(Require.TargetRaftGroupId);
            bs.Write(Require.FromRaftGroupId);
            bs.Write(Require.FromTableId);
            bs.Write(Require.Diff);

            bs.Write(Require.KeySize.ToInt32());
            var span = new ReadOnlySpan<byte>(Require.KeyPtr.ToPointer(), Require.KeySize.ToInt32());
            bs.Stream.Write(span);
        }

        public unsafe void ReadObject(BinSerializer bs)
        {
            WaitHandle = new IntPtr(bs.ReadInt64());
            TxnPtr = new IntPtr(bs.ReadInt64());

            var req = new ClrAddRefRequire();
            req.TargetRaftGroupId = bs.ReadUInt64();
            req.FromRaftGroupId = bs.ReadUInt64();
            req.FromTableId = bs.ReadUInt32();
            req.Diff = bs.ReadInt32();

            req.KeySize = new IntPtr(bs.ReadInt32());
            req.KeyPtr = Marshal.AllocHGlobal(req.KeySize.ToInt32());
            var span = new Span<byte>(req.KeyPtr.ToPointer(), req.KeySize.ToInt32());
            bs.Stream.Read(span);

            Require = req;
        }
    }

}
