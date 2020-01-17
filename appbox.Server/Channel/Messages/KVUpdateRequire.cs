#if FUTURE
using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// 用于子进程包装ClrUpdateRequire传给主进程后调用NativeApi
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KVUpdateRequire : IMessage
    {
        public ClrUpdateRequire Require { get; private set; } //注意不要调整顺序，与native一致
        public IntPtr WaitHandle { get; private set; }
        public IntPtr TxnPtr { get; private set; }

        public MessageType Type => MessageType.KVUpdateRequire;
        public PayloadType PayloadType => PayloadType.KVUpdateRequire;

        public KVUpdateRequire(IntPtr waitHandle, IntPtr txnPtr, IntPtr reqPtr)
        {
            WaitHandle = waitHandle;
            TxnPtr = txnPtr;
            unsafe
            {
                ClrUpdateRequire* req = (ClrUpdateRequire*)reqPtr.ToPointer();
                Require = *req;
            }
        }

        internal void FreeData()
        {
            if (Require.KeyPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.KeyPtr);
            if (Require.RefsPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.RefsPtr);
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(WaitHandle.ToInt64());
            bs.Write(TxnPtr.ToInt64());
            bs.Write(Require.RaftGroupId);
            bs.Write(Require.SchemaVersion);
            bs.Write((byte)Require.DataCF);
            bs.Write(Require.Merge);
            bs.Write(Require.ReturnExists);

            bs.Write(Require.KeySize.ToInt32());
            var span = new ReadOnlySpan<byte>(Require.KeyPtr.ToPointer(), Require.KeySize.ToInt32());
            bs.Stream.Write(span);

            bs.Write(Require.RefsSize.ToInt32());
            if (Require.RefsSize.ToInt32() > 0)
            {
                span = new ReadOnlySpan<byte>(Require.RefsPtr.ToPointer(), Require.RefsSize.ToInt32());
                bs.Stream.Write(span);
            }

            int dataSize = NativeBytes.GetSize(Require.DataPtr);
            bs.Write(dataSize);
            span = new ReadOnlySpan<byte>((Require.DataPtr + 4).ToPointer(), dataSize);
            bs.Stream.Write(span);
        }

        public unsafe void ReadObject(BinSerializer bs)
        {
            WaitHandle = new IntPtr(bs.ReadInt64());
            TxnPtr = new IntPtr(bs.ReadInt64());

            var req = new ClrUpdateRequire();
            req.RaftGroupId = bs.ReadUInt64();
            req.SchemaVersion = bs.ReadUInt32();
            req.DataCF = (sbyte)bs.ReadByte();
            req.Merge = bs.ReadBoolean();
            req.ReturnExists = bs.ReadBoolean();

            req.KeySize = new IntPtr(bs.ReadInt32());
            req.KeyPtr = Marshal.AllocHGlobal(req.KeySize.ToInt32());
            var span = new Span<byte>(req.KeyPtr.ToPointer(), req.KeySize.ToInt32());
            bs.Stream.Read(span);

            req.RefsSize = new IntPtr(bs.ReadInt32());
            if (req.RefsSize.ToInt32() > 0)
            {
                req.RefsPtr = Marshal.AllocHGlobal(req.RefsSize.ToInt32());
                span = new Span<byte>(req.RefsPtr.ToPointer(), req.RefsSize.ToInt32());
                bs.Stream.Read(span);
            }

            int dataSize = bs.ReadInt32();
            req.DataPtr = NativeApi.NewNativeString(dataSize, out byte* dp);
            span = new Span<byte>(dp, dataSize);
            bs.Stream.Read(span);

            Require = req;
        }
    }
}
#endif