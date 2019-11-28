using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace appbox.Caching
{
    /// <summary>
    /// 线程安全的对象池
    /// </summary>
    public sealed class ObjectPool<T> // where T : class
    {

        #region ====Fields====

        private /*volatile*/ int _readIndex;
        private /*volatile*/ int _writeIndex;
        private /*volatile*/ int _writeLock;

        private T[] _queue;
        private readonly int _queueLength;

        private Func<ObjectPool<T>, T> _generator;
        private Action<T> _cleaner;

        #endregion

        #region ====Ctor====

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBox.Core.ObjectPool`1"/> class.
        /// </summary>
        /// <param name="generator">Generator.</param>
        /// <param name="cleaner">Cleaner.</param>
        /// <param name="powerOf2Count">必须为2的n次方</param>
        public ObjectPool(Func<ObjectPool<T>, T> generator, Action<T> cleaner, int powerOf2Count)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));

            _queueLength = powerOf2Count;
            _queue = new T[_queueLength];
            _readIndex = 0;
            _writeIndex = 0;
            _writeLock = 0;

            _generator = generator;
            _cleaner = cleaner;
        }

        #endregion

        #region ====Properties====

        //        public int Left
        //        {
        //            get
        //            {
        //                //Todo:
        //                return _writeIndex - _readIndex;
        //            }
        //        }

        #endregion

        #region ====Methods====

        public T Pop()
        {
            int curReadIndex;
            int curWriteIndex;
            do
            {
                curReadIndex = Thread.VolatileRead(ref _readIndex);
                curWriteIndex = Thread.VolatileRead(ref _writeIndex);

                if (CountIndex(curReadIndex) == CountIndex(curWriteIndex))
                {
                    //Console.WriteLine("ObjectPool为空");
                    //the queue is empty
                    return _generator(this);
                }

                T v = _queue[CountIndex(curReadIndex)];
                if (Interlocked.CompareExchange(ref _readIndex, (curReadIndex + 1), curReadIndex) == curReadIndex)
                {
#if DEBUG
                    //if (v == null)
                    //{
                    //  Log.Error("ObjectPool.Pop<" + typeof(T).ToString() + ">", "取得空值 ReadIndex=" + curReadIndex.ToString() + " WriteIndex=" + curWriteIndex.ToString());
                    //  System.Environment.Exit(0);
                    //}
#endif
                    return v;
                }
            } while (true);
        }

        public void Push(T obj)
        {
            int curReadIndex;
            int curWriteIndex;
            int tryCount = 0;

            do
            {
                //注意：检查queue is full的代码必须的lock内处理
                if (Interlocked.CompareExchange(ref _writeLock, 1, 0) == 0) //enter write lock
                {
                    //Console.WriteLine("ObjectPool lock by {0}", Thread.CurrentThread.ManagedThreadId);
                    curReadIndex = Thread.VolatileRead(ref _readIndex);
                    curWriteIndex = Thread.VolatileRead(ref _writeIndex);
                    if (CountIndex(curWriteIndex + 1) == CountIndex(curReadIndex))
                    {
                        //Console.WriteLine("ObjectPool已Full释放 {0}", Thread.CurrentThread.ManagedThreadId);
                        //the queue is full
                        _cleaner?.Invoke(obj);
                    }
                    else
                    {
                        _queue[CountIndex(curWriteIndex)] = obj;
                        Interlocked.Increment(ref _writeIndex);
                    }
                    //todo:此方案在下名前线程Crash的问题,基于多生产者的性能问题考虑直接lock(writeLock)方案
                    Interlocked.Exchange(ref _writeLock, 0); //exit write lock
                    return;
                }

                //does not accuire write lock, try again
                tryCount++;
                if (tryCount > 2) //Todo:确定合理循环次数
                {
                    //Console.WriteLine("ObjectPool直接释放 {0}", Thread.CurrentThread.ManagedThreadId);
                    //直接释放资源，不再循环
                    _cleaner?.Invoke(obj);
                    return;
                }
                Thread.SpinWait(3); //Don't use Thread.Yield();
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CountIndex(int index)
        {
            return index & (_queueLength - 1);
        }

        #endregion
    }
}

