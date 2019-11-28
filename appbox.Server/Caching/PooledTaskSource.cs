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

    sealed class PooledTaskSource<T> : IValueTaskSource<T> where T : struct
    {
        private const string MultipleAwaiter = "Multiple awaiters are not allowed";

        /// Sentinel object used to indicate that the operation has completed prior to OnCompleted being called.
        private static readonly Action<object> CallbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };

        #region ====Fields & Properties====
        private readonly ObjectPool<PooledTaskSource<T>> pool;

        private Action<object> continuation;
        private T? result;
        //Exception exception;

        /// <summary>Current token value given to a ValueTask and then verified against the value it passes back to us.</summary>
        /// <remarks>
        /// This is not meant to be a completely reliable mechanism, doesn't require additional synchronization, etc.
        /// It's purely a best effort attempt to catch misuse, including awaiting for a value task twice and after
        /// it's already being reused by someone else.
        /// </remarks>
        private short token;
        private object state;

        private ExecutionContext executionContext;
        private object scheduler;

        public IntPtr GCHandlePtr { get; private set; }
        #endregion

        #region ====Ctor====
        private PooledTaskSource(ObjectPool<PooledTaskSource<T>> pool)
        {
            this.pool = pool;
        }

        public static ObjectPool<PooledTaskSource<T>> Create(int count)
        {
            var pool = new ObjectPool<PooledTaskSource<T>>(p =>
            {
                var obj = new PooledTaskSource<T>(p);
                var gcHandle = GCHandle.Alloc(obj);
                obj.GCHandlePtr = GCHandle.ToIntPtr(gcHandle);
                return obj;
            },
            t =>
            {
                var gcHandle = GCHandle.FromIntPtr(t.GCHandlePtr);
                gcHandle.Free();
            }, count);
            return pool;
        }
        #endregion

        #region ====Methods====
        public T GetResult(short token)
        {
            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId}");
            if (token != this.token)
                throw new InvalidOperationException(MultipleAwaiter);
            //var exception = this.exception;

            T res = result.Value;
            this.token++;
            result = null;
            //exception = null;
            state = null;
            continuation = null;
            pool.Push(this);

            //if (exception != null)
            //throw exception;
            return res;
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId}");
            if (token != this.token)
                throw new InvalidOperationException(MultipleAwaiter);
            if (result == null)
                return ValueTaskSourceStatus.Pending;
            //return exception != null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Faulted;
            return ValueTaskSourceStatus.Succeeded;
        }

        /// <summary>Called on awaiting so:
        /// - if operation has not yet completed - queues the provided continuation to be executed once the operation is completed
        /// - if operation has completed - 
        /// </summary>
        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId} {ReferenceEquals(continuation, CallbackCompleted)}");
            if (token != this.token)
                throw new InvalidOperationException(MultipleAwaiter);

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    scheduler = sc;
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        scheduler = ts;
                    }
                }
            }

            // Remember current state
            this.state = state;
            // Remember continuation to be executed on completed (if not already completed, in case of which
            // continuation will be set to CallbackCompleted)
            var previousContinuation = Interlocked.CompareExchange(ref this.continuation, continuation, null);
            if (previousContinuation != null)
            {
                if (!ReferenceEquals(previousContinuation, CallbackCompleted))
                    throw new InvalidOperationException(MultipleAwaiter);

                // Lost the race condition and the operation has now already completed.
                // We need to invoke the continuation, but it must be asynchronously to
                // avoid a stack dive.  However, since all of the queueing mechanisms flow
                // ExecutionContext, and since we're still in the same context where we
                // captured it, we can just ignore the one we captured.
                executionContext = null;
                this.state = null; // we have the state in "state"; no need for the one in UserToken
                InvokeContinuation(continuation, state, forceAsync: true);
            }
        }

        public ValueTask<T> WaitAsync()
        {
            //注意：原实现断言后启动实际操作，现实现先启动，可能实际操作已完成，所以不需要断言
            //Debug.Assert(Volatile.Read(ref continuation) == null, $"Expected null continuation to indicate reserved for use");
            //if (Volatile.Read(ref continuation) != null)
            //Log.Debug($"Expected null continuation to indicate reserved for use: {ReferenceEquals(continuation, CallbackCompleted)}");
            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId}");

            // Opearation not yet completed. Return ValueTask wrapping us.
            return new ValueTask<T>(this, token);
        }

        /// <summary>
        /// 用于消息队列收到消息后强制在线程池处理消息
        /// </summary>
        internal bool NotifyCompletionOnThreadPool(T msg)
        {
            result = msg;
            var previousContinuation = Interlocked.CompareExchange(ref continuation, CallbackCompleted, null);
            if (previousContinuation != null)
            {
                executionContext = null;
                scheduler = null;
                ThreadPool.QueueUserWorkItem(previousContinuation, state, preferLocal: false);
                //ThreadPool.UnsafeQueueUserWorkItem(previousContinuation, state);
            }
            return true; //TODO: fix无法加入线程池处理
        }

        internal void NotifyCompletion(T msg /*, Exception ex = null*/)
        {
            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId}");
            result = msg;

            // Mark operation as completed
            var previousContinuation = Interlocked.CompareExchange(ref continuation, CallbackCompleted, null);
            if (previousContinuation != null)
            {
                //Log.Debug($"Async work complete, token={token}");
                // Async work completed, continue with... continuation
                ExecutionContext ec = executionContext;
                if (ec == null)
                {
                    InvokeContinuation(previousContinuation, this.state, forceAsync: false);
                }
                else
                {
                    // This case should be relatively rare, as the async Task/ValueTask method builders
                    // use the awaiter's UnsafeOnCompleted, so this will only happen with code that
                    // explicitly uses the awaiter's OnCompleted instead.
                    executionContext = null;
                    ExecutionContext.Run(ec, runState =>
                    {
                        var t = (Tuple<PooledTaskSource<T>, Action<object>, object>)runState;
                        t.Item1.InvokeContinuation(t.Item2, t.Item3, forceAsync: false);
                    }, Tuple.Create(this, previousContinuation, state));
                }
            }
        }

        private void InvokeContinuation(Action<object> continuation, object state, bool forceAsync)
        {
            if (continuation == null)
                return;

            //Log.Debug($"token={token} thread={Thread.CurrentThread.ManagedThreadId} {ReferenceEquals(continuation, CallbackCompleted)}, forceAsync={forceAsync}");

            object scheduler = this.scheduler;
            this.scheduler = null;
            if (scheduler != null)
            {
                if (scheduler is SynchronizationContext sc)
                {
                    sc.Post(s =>
                    {
                        var t = (Tuple<Action<object>, object>)s;
                        t.Item1(t.Item2);
                    }, Tuple.Create(continuation, state));
                }
                else
                {
                    Debug.Assert(scheduler is TaskScheduler, $"Expected TaskScheduler, got {scheduler}");
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, (TaskScheduler)scheduler);
                }
            }
            else if (forceAsync)
            {
                ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
            }
            else
            {
                continuation(state);
            }
        }
        #endregion
    }
}
