using System;

namespace appbox.Serialization
{
    sealed class ByteSerializer : TypeSerializer
    {
        public ByteSerializer() : base(PayloadType.Byte, typeof(Byte))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            bs.Stream.WriteByte((byte)instance);
        }

        public override object Read(BinSerializer bs, object instance)
        {
            int res = bs.Stream.ReadByte();
            if (res < 0)
                throw new SerializationException(SerializationError.NothingToRead);

            return (byte)res;
        }
    }
}

