using System;
using System.Buffers;

namespace appbox.Logging
{
    public sealed class ConsoleLogProvider : ILogProvider
    {
        //1B5B33316D [=5B 3=33 1=31 m=6D
        private static readonly byte[] Red = { 0x1B, 0x5B, 0x33, 0x31, 0x6D };
        private static readonly byte[] Green = { 0x1B, 0x5B, 0x33, 0x32, 0x6D };
        private static readonly byte[] Yellow = { 0x1B, 0x5B, 0x33, 0x33, 0x6D };
        private static readonly byte[] Blue = { 0x1B, 0x5B, 0x33, 0x34, 0x6D };
        private static readonly byte[] Magenta = { 0x1B, 0x5B, 0x33, 0x35, 0x6D };
        private static readonly byte[] Reset = { 0x1B, 0x5b, 0x30, 0x6D };

        private static char GetLevelChar(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return 'D';
                case LogLevel.Info: return 'I';
                case LogLevel.Warn: return 'W';
                case LogLevel.Error: return 'E';
                default: return 'U';
            }
        }

        private static byte[] GetLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return Blue;
                case LogLevel.Info: return Green;
                case LogLevel.Warn: return Yellow;
                case LogLevel.Error: return Red;
                default: return Magenta;
            }
        }

        public void Write(LogLevel level, string file, int line, string method, string msg)
        {
            //TODO:暂先简单实现，待优化
#if Windows
            Console.WriteLine("[{0}{1:MM}{1:dd} {1:hh:mm:ss} {2}.{3}:{4}]: {5}",
                GetLevelChar(level), DateTime.Now, file, method, line, msg);
#else
            var now = DateTime.Now;
            var head = string.Format("[{0}{1:MM}{1:dd} {1:hh:mm:ss} {2}.{3}:{4}]: ",
                GetLevelChar(level), now, file, method, line);

            int headerSize = 0;
            StringHelper.WriteTo(head, b => headerSize++);
            int logSize = 0;
            StringHelper.WriteTo(msg, b => logSize++);

            int totalSize = 5 + headerSize + 4 + logSize + 1;
            var buf = ArrayPool<byte>.Shared.Rent(totalSize);
            var color = GetLevelColor(level);
            color.AsSpan().CopyTo(buf.AsSpan(0, 5));
            int headerIndex = 5;
            StringHelper.WriteTo(head, b => buf[headerIndex++] = b);
            Reset.AsSpan().CopyTo(buf.AsSpan(5 + headerSize, 4));
            int logIndex = 5 + headerSize + 4;
            StringHelper.WriteTo(msg, b => buf[logIndex++] = b);
            buf[totalSize - 1] = 0x0A; //换行

            using (var output = Console.OpenStandardOutput())
            {
                output.Write(buf, 0, totalSize);
            }
            ArrayPool<byte>.Shared.Return(buf);
#endif
        }
    }
}

