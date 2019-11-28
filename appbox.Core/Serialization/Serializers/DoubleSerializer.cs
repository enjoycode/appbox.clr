using System;

namespace appbox.Serialization
{
	public sealed class DoubleSerializer : TypeSerializer
	{
		public DoubleSerializer() : base(PayloadType.Double, typeof(double))
		{
		}

		public override void Write(BinSerializer bs, object instance)
		{
            Write(bs, (double)instance);
		}

		public void Write(BinSerializer bs, double instance)
		{
			unsafe
			{
				byte* p = (byte*)&instance;
				for (int i = 0; i < 8; i++)
				{
					bs.Stream.WriteByte(p[i]);
				}
			}
		}

		public override object Read(BinSerializer bs, object instance)
		{
			return Read(bs);
		}

		public double Read(BinSerializer bs)
		{
			double res;
			unsafe
			{
				byte* p = (byte*)&res;
				for (int i = 0; i < 8; i++)
				{
					p[i] = (byte)bs.Stream.ReadByte();
				}
			}
			return res;
		}
	}
}

