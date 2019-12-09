using System;
using System.Collections.Generic;

namespace appbox.Caching
{
    public struct CharsKey : IEquatable<CharsKey>
    {
        public ReadOnlyMemory<char> Memory { get; private set; }

        public CharsKey(string key)
        {
            Memory = key.AsMemory();
        }

        public CharsKey(ReadOnlyMemory<char> key)
        {
            Memory = key;
        }

        public override int GetHashCode()
        {
            var span = Memory.Span;
            int hash = span[0];
            for (int i = 1; i < span.Length; i++)
            {
                hash = ((hash << 5) + hash) ^ span[i];
            }
            return hash ^ span.Length;
        }

        public bool Equals(CharsKey other)
        {
            return Memory.Length == other.Memory.Length
                && Memory.Span.SequenceEqual(other.Memory.Span);
        }

        public static implicit operator CharsKey(string v)
        {
            return new CharsKey(v);
        }

        public static implicit operator CharsKey(ReadOnlyMemory<char> v)
        {
            return new CharsKey(v);
        }
    }

    //public sealed class CharsKeyEqualityComparer : IEqualityComparer<CharsKey>
    //{

    //    public bool Equals(CharsKey x, CharsKey y)
    //    {
    //        return x.Memory.Length == y.Memory.Length
    //            && x.Memory.Span.SequenceEqual(y.Memory.Span);
    //    }

    //    public int GetHashCode(CharsKey obj)
    //    {
    //        return obj.GetHashCode();
    //    }
    //}

}
