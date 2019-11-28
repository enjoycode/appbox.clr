using System;

namespace appbox.Serialization
{
    internal sealed class Int32Serializer : TypeSerializer
    {

        public Int32Serializer() : base(PayloadType.Int32, typeof(Int32))
        {}

        public override object Read(BinSerializer bs, object instance)
        {
            return VariantHelper.ReadInt32(bs.Stream);
        }

        public int Read(BinSerializer bs)
        {
            return VariantHelper.ReadInt32(bs.Stream);
        }
           
        public void Write(BinSerializer bs, int obj)
        {
            VariantHelper.WriteInt32(obj, bs.Stream);
        }

        public override void Write(BinSerializer bs, object obj)
        {
            VariantHelper.WriteInt32((int)obj, bs.Stream);
        }
    }
}

