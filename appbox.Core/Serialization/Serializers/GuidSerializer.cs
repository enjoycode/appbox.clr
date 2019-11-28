using System;

namespace appbox.Serialization
{
    internal sealed class GuidSerializer : TypeSerializer
    {
        public GuidSerializer() : base(PayloadType.Guid, typeof(Guid))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            Write(bs, (Guid)instance);
        }

        public void Write(BinSerializer bs, Guid instance)
        {
            unsafe
            {
                byte* p = (byte*)&instance;
                for (int i = 0; i < 16; i++)
                {
                    bs.Stream.WriteByte(p[i]);
                }
            }
        }

        public override object Read(BinSerializer bs, object instance)
        {
            return Read(bs);
        }

        public Guid Read(BinSerializer bs)
        {
            Guid res;
            unsafe
            {
                byte* p = (byte*)&res;
                for (int i = 0; i < 16; i++)
                {
                    p[i] = (byte)bs.Stream.ReadByte();
                }
            }
            return res;
        }
    }
}

