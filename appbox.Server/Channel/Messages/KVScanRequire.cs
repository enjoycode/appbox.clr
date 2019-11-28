using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KVScanRequire : IMessage
    {
        public ClrScanRequire Require { get; private set; } //注意不要调整顺序，与native一致
        public IntPtr WaitHandle { get; private set; }

        public MessageType Type => MessageType.KVScanRequire;
        public PayloadType PayloadType => PayloadType.KVScanRequire;

        public KVScanRequire(IntPtr waitHandle, IntPtr reqPtr)
        {
            WaitHandle = waitHandle;
            unsafe
            {
                ClrScanRequire* req = (ClrScanRequire*)reqPtr.ToPointer();
                Require = *req;
            }
        }

        /// <summary>
        /// 仅用于接收方(Host进程)在调用NativeApi后释放Key, FilterPtr不需要释放
        /// </summary>
        public void FreeKeysData()
        {
            if (Require.BeginKeyPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.BeginKeyPtr);
            if (Require.EndKeyPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.EndKeyPtr);
        }

        /// <summary>
        /// 仅用于发送方(子进程)在发送至Host进程后释放Filter数据, Keys不需要释放
        /// </summary>
        public void FreeFilterData()
        {
            if (Require.FilterPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(Require.FilterPtr);
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(WaitHandle.ToInt64());
            bs.Write(Require.RaftGroupId);
            bs.Write(Require.DataCF);
            bs.Write(Require.Skip);
            bs.Write(Require.Take);
            bs.Write(Require.ToIndexTarget);

            bs.Write(Require.BeginKeySize.ToInt32());
            var span = new ReadOnlySpan<byte>(Require.BeginKeyPtr.ToPointer(), Require.BeginKeySize.ToInt32());
            bs.Stream.Write(span);

            bs.Write(Require.EndKeySize.ToInt32());
            if (Require.EndKeyPtr != IntPtr.Zero)
            {
                span = new ReadOnlySpan<byte>(Require.EndKeyPtr.ToPointer(), Require.EndKeySize.ToInt32());
                bs.Stream.Write(span);
            }

            var filterSize = Require.FilterPtr == IntPtr.Zero ? 0 : NativeBytes.GetSize(Require.FilterPtr);
            bs.Write(filterSize);
            if (filterSize > 0)
            {
                span = new ReadOnlySpan<byte>((Require.FilterPtr + 4).ToPointer(), filterSize);
                bs.Stream.Write(span);
            }
        }

        public unsafe void ReadObject(BinSerializer bs)
        {
            WaitHandle = new IntPtr(bs.ReadInt64());

            var req = new ClrScanRequire();
            req.RaftGroupId = bs.ReadUInt64();
            req.DataCF = bs.ReadInt32();
            req.Skip = bs.ReadUInt32();
            req.Take = bs.ReadUInt32();
            req.ToIndexTarget = bs.ReadBoolean();

            req.BeginKeySize = new IntPtr(bs.ReadInt32());
            req.BeginKeyPtr = Marshal.AllocHGlobal(req.BeginKeySize.ToInt32());
            var span = new Span<byte>(req.BeginKeyPtr.ToPointer(), req.BeginKeySize.ToInt32());
            bs.Stream.Read(span);

            req.EndKeySize = new IntPtr(bs.ReadInt32());
            if(req.EndKeySize.ToInt32() > 0)
            {
                req.EndKeyPtr = Marshal.AllocHGlobal(req.EndKeySize.ToInt32());
                span = new Span<byte>(req.EndKeyPtr.ToPointer(), req.EndKeySize.ToInt32());
                bs.Stream.Read(span);
            }

            var filterSize = bs.ReadInt32();
            if (filterSize > 0)
            {
                req.FilterPtr = NativeApi.NewNativeString(filterSize, out byte* dp); //注意转换
                span = new Span<byte>(dp, filterSize);
                bs.Stream.Read(span);
            }
            Require = req;
        }
    }
}
