using System;
using System.Collections.Concurrent;

namespace appbox.Caching
{
    /// <summary>
    /// 线程安全的对象缓存
    /// </summary>
    public sealed class ObjectCache<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> caches;
        private readonly Func<TKey, TValue> loader;

        public ObjectCache(Func<TKey, TValue> loader)
        {
            caches = new ConcurrentDictionary<TKey, TValue>();
            this.loader = loader;
        }

        public TValue GetObject(TKey key)
        {
            return caches.GetOrAdd(key, loader);
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            caches.AddOrUpdate(key, value, (k, oldValue) => value);
        }

        /// <summary>
        /// 如果Key存在则更新，否则不做操作
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void TryUpdate(TKey key, TValue value)
        {
            if (caches.ContainsKey(key))
            {
                caches.AddOrUpdate(key, value, (k, oldValue) => value);
            }
        }

        public void TryRemove(TKey key)
        {
            TValue removed;
            caches.TryRemove(key, out removed);
        }

        public bool Contains(TKey key)
        {
            return caches.ContainsKey(key);
        }

        public void Clear()
        {
            caches.Clear();
        }

    }
}
