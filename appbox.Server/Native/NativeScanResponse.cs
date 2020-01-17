#if FUTURE
using System;
using appbox.Server;

namespace appbox.Server
{
    sealed class NativeScanResponse : IDisposable, IScanResponse
    {
        private readonly IntPtr _handle;
        public uint Skipped => NativeApi.ScanResponseGetSkipped(_handle);
        public int Length { get; }

        //TODO: property Skipped for partitioned scan

        internal NativeScanResponse(IntPtr handle, int length)
        {
            _handle = handle;
            Length = length;
        }

        public void ForEachRow(Action<IntPtr, int, IntPtr, int> action)
        {
            if (Length <= 0)
                return;

            IteratorKV kv = new IteratorKV();
            IntPtr kvPtr = IntPtr.Zero;
            unsafe
            {
                kvPtr = new IntPtr(&kv);
            }

            for (int i = 0; i < Length; i++)
            {
                NativeApi.ScanResponseGetKV(_handle, i, kvPtr);
                action(kv.KeyPtr, kv.KeySize.ToInt32(), kv.ValuePtr, kv.ValueSize.ToInt32());
            }
        }

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeApi.FreeScanResponse(_handle);
                disposedValue = true;
            }
        }

        ~NativeScanResponse()
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