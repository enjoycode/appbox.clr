#if FUTURE
using System;
using appbox.Serialization;

namespace appbox.Server
{
    public struct CommitTranRequire : IMessage
    {
        public IntPtr TxnPtr { get; private set; }
        public IntPtr WaitHandle { get; private set; }

        public MessageType Type => MessageType.CommitTranRequire;
        public PayloadType PayloadType => PayloadType.CommitTranRequire;

        public CommitTranRequire(IntPtr txnPtr, IntPtr waitHandle)
        {
            TxnPtr = txnPtr;
            WaitHandle = waitHandle;
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            long hv = TxnPtr.ToInt64();
            var span = new Span<byte>(&hv, 8);
            bs.Stream.Write(span);
            hv = WaitHandle.ToInt64();
            span = new Span<byte>(&hv, 8);
            bs.Stream.Write(span);
        }

        public void ReadObject(BinSerializer reader)
        {
            throw new NotSupportedException();
        }

        internal unsafe void FastReadFrom(MessageChunk* first)
        {
            byte* dataPtr = MessageChunk.GetDataPtr(first);
            long* src = (long*)(dataPtr + 1);
            TxnPtr = new IntPtr(*src);
            src = (long*)(dataPtr + 9);
            WaitHandle = new IntPtr(*src);
        }

    }
}
#endif
