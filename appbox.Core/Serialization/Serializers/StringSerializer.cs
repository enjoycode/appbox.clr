using System;

namespace appbox.Serialization
{
    internal sealed class StringSerializer : TypeSerializer
    {
        public StringSerializer() : base(PayloadType.String, typeof(String))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            Write(bs, (string)instance);
        }

        public void Write(BinSerializer bs, string instance)
        {
            if (instance == null)
            {
                VariantHelper.WriteInt32(-1, bs.Stream);
            }
            else if (instance == string.Empty)
            {
                VariantHelper.WriteInt32(0, bs.Stream);
            }
            else
            {
                VariantHelper.WriteInt32(instance.Length, bs.Stream);
                StringHelper.WriteTo(instance, b => bs.Stream.WriteByte(b));
            }
        }

        public override object Read(BinSerializer bs, object instance)
        {
            return Read(bs);
        }

        public string Read(BinSerializer bs)
        {
            int len = VariantHelper.ReadInt32(bs.Stream);
            if (len == -1)
                return null;
            if (len == 0)
                return string.Empty;
            return StringHelper.ReadFrom(len, () => (byte)bs.Stream.ReadByte());
        }
    }
}

