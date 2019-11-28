using System;

namespace appbox.Serialization
{
    internal sealed class DateTimeSerializer : TypeSerializer
    {
        public DateTimeSerializer() : base(PayloadType.DateTime, typeof(DateTime))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            VariantHelper.WriteInt64(((DateTime)instance).ToUniversalTime().Ticks, bs.Stream);
        }

        public void Write(BinSerializer bs, DateTime instance)
        {
            VariantHelper.WriteInt64(instance.ToUniversalTime().Ticks, bs.Stream);
        }

        public override object Read(BinSerializer bs, object instance)
        {
            return new DateTime(VariantHelper.ReadInt64(bs.Stream), DateTimeKind.Utc).ToLocalTime();
        }

        public DateTime Read(BinSerializer bs)
        {
            return new DateTime(VariantHelper.ReadInt64(bs.Stream), DateTimeKind.Utc).ToLocalTime();
        }
    }
}

