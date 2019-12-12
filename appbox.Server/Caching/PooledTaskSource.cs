using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using appbox.Caching;

namespace appbox.Server
{
    //http://tooslowexception.com/implementing-custom-ivaluetasksource-async-without-allocations/
    //NetCore3 System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore

    sealed class PooledTaskSource<T> : IValueTaskSource<T>, IDisposable where T : struct
    {

        public static ObjectPool<PooledTaskSource<T>> Create(int count)
        {
            var pool = new ObjectPool<PooledTaskSource<T>>(() =>
            {
                var obj = new PooledTaskSource<T>();
                var gcHandle = GCHandle.Alloc(obj);
                obj.GCHandlePtr = GCHandle.ToIntPtr(gcHandle);
                return obj;
            }, count);
            return pool;
        }

        private ManualResetValueTaskSourceCore<T> tsc;
        internal IntPtr GCHandlePtr { get; private set; }

        private PooledTaskSource()
        {
            tsc.RunContinuationsAsynchronously = true;
        }

        public T GetResult(short token)
        {
            var res = tsc.GetResult(token);
            tsc.Reset();
            return res;
        }

        public ValueTaskSourceStatus GetStatus(short token) => tsc.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
                                => tsc.OnCompleted(continuation, state, token, flags);

        public ValueTask<T> WaitAsync()
        {
            //TODO:shotpath for completed?
            return new ValueTask<T>(this, tsc.Version);
        }

        /// <summary>
        /// 适用于EventLoop线程收到消息后在其他线程处理
        /// </summary>
        public bool SetResultOnOtherThread(T result)
        {
            try
            {
                var ok = ThreadPool.QueueUserWorkItem<T>(res => tsc.SetResult(res), result, preferLocal: false);
                return ok;
            }
            catch (NotSupportedException)
            {
                Log.Warn("Enqueue thread pool error");
                return false;
            }
        }

        public void SetResult(T result) => tsc.SetResult(result);

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool _)
        {
            if (!disposedValue)
            {
                if (GCHandlePtr != IntPtr.Zero)
                {
                    var gcHandle = GCHandle.FromIntPtr(GCHandlePtr);
                    gcHandle.Free();
                    GCHandlePtr = IntPtr.Zero;
                }

                disposedValue = true;
            }
        }

        ~PooledTaskSource()
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
