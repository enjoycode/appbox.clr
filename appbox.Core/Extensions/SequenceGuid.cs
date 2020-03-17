using System.Security.Cryptography;
using System.Threading;

namespace System
{
    public static class SequenceGuid
    {

        private static int _inc = 0;

        static SequenceGuid()
        {
            byte[] rng = new byte[4];
            using (var rand = new RNGCryptoServiceProvider())
            {
                rand.GetBytes(rng);
            }
            int v = BitConverter.ToInt32(rng, 0);
            Interlocked.Exchange(ref _inc, v);
        }

        /// <summary>
        /// 获取顺序Guid，线程安全
        /// </summary>
        public unsafe static Guid NewGuid()
        {
            long ticks = DateTime.UtcNow.Ticks;
            int rng = Interlocked.Increment(ref _inc);

            return new Guid(appbox.Runtime.RuntimeContext.PeerId,
                (short)((ticks >> 48) & 0xFFFF),
                (short)((ticks >> 32) & 0xFFFF),
                (byte)((ticks >> 24) & 0xFF),
                (byte)((ticks >> 16) & 0xFF),
                (byte)((ticks >> 8) & 0xFF),
                (byte)(ticks & 0xFF),
                (byte)((rng >> 24) & 0xFF),
                (byte)((rng >> 16) & 0xFF),
                (byte)((rng >> 8) & 0xFF),
                (byte)(rng & 0xFF));
        }

    }

    
}
