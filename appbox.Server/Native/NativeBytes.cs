#if FUTURE
using System;
using System.Runtime.InteropServices;

namespace appbox.Server
{
    //注意:头4字节表示长度,为了跟NativeString一致
    public sealed class NativeBytes : INativeData, IDisposable
    {
        internal IntPtr RawPtr { get; }

        public ulong Size => (ulong)GetSize(RawPtr);

        public IntPtr DataPtr => RawPtr + 4;

        internal NativeBytes(int size)
        {
            RawPtr = MakeRaw(size);
        }

        internal NativeBytes(IntPtr ptr)
        {
            RawPtr = ptr;
        }

        internal static IntPtr MakeRaw(int size)
        {
            IntPtr rawPtr = Marshal.AllocHGlobal(size + 4);
            unsafe
            {
                int* ptr = (int*)rawPtr.ToPointer();
                *ptr = size;
            }
            return rawPtr;
        }

        internal static int GetSize(IntPtr rawPtr)
        {
            unsafe
            {
                int* ptr = (int*)rawPtr.ToPointer();
                return *ptr;
            }
        }

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Marshal.FreeHGlobal(RawPtr);
                disposedValue = true;
            }
        }

        ~NativeBytes()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
#endif