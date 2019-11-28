using System;

namespace appbox.Serialization
{
	public sealed class FloatSerializer : TypeSerializer
	{
		public FloatSerializer() : base(PayloadType.Float, typeof(float))
        { }

		public override void Write(BinSerializer bs, object instance)
		{
            Write(bs, (float)instance);
		}

		public void Write(BinSerializer bs, float instance)
		{
			unsafe
			{
				byte* p = (byte*)&instance;
				for (int i = 0; i < 4; i++)
				{
					bs.Stream.WriteByte(p[i]);
				}
			}
		}

		public override object Read(BinSerializer bs, object instance)
		{
			return Read(bs);
		}

		public float Read(BinSerializer bs)
		{
			float res;
			unsafe
			{
				byte* p = (byte*)&res;
				for (int i = 0; i < 4; i++)
				{
					p[i] = (byte)bs.Stream.ReadByte();
				}
			}
			return res;
		}
	}
}

