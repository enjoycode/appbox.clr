using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using CSharpTest.Net.Collections;

namespace appbox.Caching
{
    public sealed class LRUCache<TKey, TValue>
    {
        readonly LurchTable<TKey, TValue> cache;

        public int Count => cache.Count;

        public LRUCache(int limit)
        {
            cache = new LurchTable<TKey, TValue>(LurchTableOrder.Access, limit);
        }

        public LRUCache(int limit, IEqualityComparer<TKey> comparer)
        {
            cache = new LurchTable<TKey, TValue>(LurchTableOrder.Access, limit, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(TKey key, out TValue value)
        {
            return cache.TryGetValue(key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, TValue value)
        {
            return cache.TryAdd(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemove(TKey key)
        {
            return cache.TryRemove(key, out _);
        }
    }
}
