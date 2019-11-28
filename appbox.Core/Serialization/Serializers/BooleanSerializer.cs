using System;

namespace appbox.Serialization
{
    sealed class BooleanSerializer : TypeSerializer
    {
        public BooleanSerializer() : base(PayloadType.Boolean, typeof(Boolean))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            bool value = (bool)instance;
            if (value)
                bs.Stream.WriteByte((byte)1);
            else
                bs.Stream.WriteByte((byte)0);
        }

        public override object Read(BinSerializer bs, object instance)
        {
            int res = bs.Stream.ReadByte();
            if (res < 0)
                throw new SerializationException(SerializationError.NothingToRead);

            return res == 0 ? false : true;
        }
    }
}

