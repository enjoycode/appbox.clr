#if FUTURE
using System;
using appbox.Serialization;

namespace appbox.Server
{
    public struct BeginTranRequire : IMessage
    {
        public bool ReadCommitted { get; private set; }
        public IntPtr WaitHandle { get; private set; }

        public MessageType Type => MessageType.BeginTranRequire;
        public PayloadType PayloadType => PayloadType.BeginTranRequire;

        public BeginTranRequire(bool readCommitted, IntPtr waitHandle)
        {
            ReadCommitted = readCommitted;
            WaitHandle = waitHandle;
        }

        public unsafe void WriteObject(BinSerializer bs)
        {
            bs.Write(ReadCommitted);
            long hv = WaitHandle.ToInt64();
            var span = new Span<byte>(&hv, 8);
            bs.Stream.Write(span);
        }

        public void ReadObject(BinSerializer reader)
        {
            throw new NotSupportedException();
        }

        internal unsafe void FastReadFrom(MessageChunk* first)
        {
            byte* dataPtr = MessageChunk.GetDataPtr(first);
            ReadCommitted = dataPtr[1] > 0;
            long* src = (long*)(dataPtr + 2);
            WaitHandle = new IntPtr(*src);
        }
    }
}
#endif
