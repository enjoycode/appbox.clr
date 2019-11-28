using System;

namespace appbox.Server
{
    /// <summary>
    /// 包装C++ std::string
    /// </summary>
    public sealed class NativeString : IDisposable, INativeData
    {
        private readonly IntPtr _handle;

        public ulong Size => NativeApi.GetStringSize(_handle);
        public IntPtr DataPtr => NativeApi.GetStringData(_handle);

        internal NativeString(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new Exception("NativeString handle is null");
            _handle = handle;
        }

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeApi.FreeNativeString(_handle);
                disposedValue = true;
            }
        }

        ~NativeString()
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
