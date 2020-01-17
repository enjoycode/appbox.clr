#if FUTURE
using System;
using System.IO;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// 仅用于子进程接收主进程的结果
    /// </summary>
    sealed class RemoteScanResponse : IScanResponse, IDisposable
    {
        private readonly IntPtr _rawPtr;

        public int Length { get; }
        public uint Skipped => GetSkipped();

        internal RemoteScanResponse(IntPtr rawPtr, int length)
        {
            _rawPtr = rawPtr;
            Length = length;
        }

        public void ForEachRow(Action<IntPtr, int, IntPtr, int> action)
        {
            if (Length <= 0)
                return;

            unsafe
            {
                byte* dataPtr = (byte*)_rawPtr.ToPointer();
                dataPtr += 4; //skipped
                int* dataSizePtr;

                IntPtr keyPtr;
                int keySize;
                IntPtr valuePtr;
                int valueSize;
                for (int i = 0; i < Length; i++)
                {
                    dataSizePtr = (int*)dataPtr;
                    keySize = *dataSizePtr;
                    keyPtr = new IntPtr(dataPtr + 4);
                    dataPtr += 4 + keySize;

                    dataSizePtr = (int*)dataPtr;
                    valueSize = *dataSizePtr;
                    valuePtr = new IntPtr(dataPtr + 4);
                    dataPtr += 4 + valueSize;

                    action(keyPtr, keySize, valuePtr, valueSize);
                }
            }
        }

        private uint GetSkipped()
        {
            unsafe
            {
                uint* ptr = (uint*)_rawPtr.ToPointer();
                return *ptr;
            }
        }

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Marshal.FreeHGlobal(_rawPtr);
                disposedValue = true;
            }
        }

        ~RemoteScanResponse()
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