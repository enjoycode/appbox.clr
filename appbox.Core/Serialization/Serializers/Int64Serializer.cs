using System;

namespace appbox.Serialization
{
    internal sealed class Int64Serializer : TypeSerializer
    {

        public Int64Serializer() : base(PayloadType.Int64, typeof(Int64))
        {}

        public override object Read(BinSerializer bs, object instance)
        {
            return VariantHelper.ReadInt64(bs.Stream);
        }

        public long Read(BinSerializer bs)
        {
            return VariantHelper.ReadInt64(bs.Stream);
        }
           
        public void Write(BinSerializer bs, long obj)
        {
            VariantHelper.WriteInt64(obj, bs.Stream);
        }

        public override void Write(BinSerializer bs, object obj)
        {
            VariantHelper.WriteInt64((long)obj, bs.Stream);
        }
    }
}

