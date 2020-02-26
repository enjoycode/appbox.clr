using System;

namespace appbox.Serialization
{
    internal sealed class Int64Serializer : TypeSerializer
    {

        public Int64Serializer() : base(PayloadType.Int64, typeof(long)) { }

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

    internal sealed class UInt64Serializer : TypeSerializer
    {

        public UInt64Serializer() : base(PayloadType.UInt64, typeof(ulong)) { }

        public override object Read(BinSerializer bs, object instance)
        {
            return VariantHelper.ReadUInt64(bs.Stream);
        }

        public ulong Read(BinSerializer bs)
        {
            return VariantHelper.ReadUInt64(bs.Stream);
        }

        public void Write(BinSerializer bs, ulong obj)
        {
            VariantHelper.WriteUInt64(obj, bs.Stream);
        }

        public override void Write(BinSerializer bs, object obj)
        {
            VariantHelper.WriteUInt64((ulong)obj, bs.Stream);
        }
    }
}

