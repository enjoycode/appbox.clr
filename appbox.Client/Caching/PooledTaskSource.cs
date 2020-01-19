using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace appbox.Caching
{
    sealed class PooledTaskSource<T> : IValueTaskSource<T>
    {

        public static ObjectPool<PooledTaskSource<T>> Create(int count)
        {
            return new ObjectPool<PooledTaskSource<T>>(() => new PooledTaskSource<T>(), count);
        }

        private ManualResetValueTaskSourceCore<T> tsc;

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

        public void SetResult(T result) => tsc.SetResult(result);

    }
}
