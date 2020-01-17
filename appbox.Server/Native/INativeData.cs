#if FUTURE
using System;

namespace appbox.Server
{
    public interface INativeData : IDisposable
    {
        ulong Size { get; }
        IntPtr DataPtr { get; }
    }

    public static class NativeDataExtensions
    {
        public static string ToHexString(this INativeData data)
        {
            return StringHelper.ToHexString(data.DataPtr, (int)data.Size);
        }

        public unsafe static ulong ToUInt64(this INativeData data)
        {
            if (data.Size != 8)
                throw new InvalidCastException();

            ulong* ptr = (ulong*)data.DataPtr.ToPointer();
            return *ptr;
        }
    }
}
#endif
