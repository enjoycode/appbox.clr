#if FUTURE
using System;
using appbox.Serialization;

namespace appbox.Server
{
    public struct RollbackTranRequire : IMessage
    {
        public IntPtr TxnPtr { get; private set; }
        public bool IsAbort { get; private set; }

        public MessageType Type => MessageType.RollbackTranRequire;
        public PayloadType PayloadType => PayloadType.RollbackTranRequire;

        public RollbackTranRequire(IntPtr txnPtr, bool isAbort)
        {
            TxnPtr = txnPtr;
            IsAbort = isAbort;
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            long hv = TxnPtr.ToInt64();
            var span = new Span<byte>(&hv, 8);
            bs.Stream.Write(span);
            bs.Write(IsAbort);
        }

        public void ReadObject(BinSerializer reader)
        {
            throw new NotSupportedException();
        }

        internal unsafe void FastReadFrom(MessageChunk* first)
        {
            byte* dataPtr = MessageChunk.GetDataPtr(first) + 1; //offset 1 byte type
            long* src = (long*)(dataPtr);
            TxnPtr = new IntPtr(*src);
            IsAbort = dataPtr[8] != 0;
        }
    }
}
#endif
