using System;
using System.Collections.Generic;

namespace appbox.Caching
{

    /// <summary>
    /// 仅用于作为缓存键，以防止不必要的堆内存分配，分两种类型:
    /// 1. Unmanaged, 临时用,不用管内存释放，可以转换为Managed
    /// 2. Managed, 作为缓存键，只能由Unmanaged拷贝而来
    /// </summary>
    public struct BytesKey
    {
        internal readonly IntPtr unmanagedPtr;
        internal readonly int unmanagedSize;
        internal byte[] managed;

        public int Length => managed == null ? unmanagedSize : managed.Length;
        public ReadOnlySpan<byte> Span
        {
            get
            {
                unsafe
                {
                    return managed == null ?
                        new ReadOnlySpan<byte>(unmanagedPtr.ToPointer(), unmanagedSize)
                            : new ReadOnlySpan<byte>(managed);
                }
            }
        }

        internal string DebugString
        {
            get
            {
                return managed == null ?
                    StringHelper.ToHexString(unmanagedPtr, unmanagedSize)
                            : StringHelper.ToHexString(managed);
            }
        }

        /// <summary>
        /// Initializes a new unmanaged instance
        /// </summary>
        public BytesKey(IntPtr ptr, int size)
        {
            unmanagedPtr = ptr;
            unmanagedSize = size;
            managed = null;
        }

        /// <summary>
        /// Initializes a new managed instance
        /// </summary>
        internal BytesKey(byte[] data)
        {
            unmanagedPtr = IntPtr.Zero;
            unmanagedSize = 0;
            managed = data;
        }

        public BytesKey CopyToManaged()
        {
            byte[] data = new byte[unmanagedSize];
            System.Runtime.InteropServices.Marshal.Copy(unmanagedPtr, data, 0, unmanagedSize);
            //unsafe
            //{
            //    fixed (byte* ptr = data)
            //    {
            //        NativeMemory.Copy(new IntPtr(ptr), unmanagedPtr, unmanagedSize);
            //    }
            //}
            return new BytesKey(data);
        }
    }

    public sealed class BytesKeyEqualityComparer : IEqualityComparer<BytesKey>
    {
        private BytesKeyEqualityComparer() { }
        public static readonly BytesKeyEqualityComparer Default = new BytesKeyEqualityComparer();

        public bool Equals(BytesKey x, BytesKey y)
        {
            return x.Length == y.Length && x.Span.SequenceEqual(y.Span); //TODO: use native memcmp?
        }

        public unsafe int GetHashCode(BytesKey obj)
        {
            //TODO: check and fix
            if (obj.managed == null)
            {
                byte* ptr = (byte*)obj.unmanagedPtr.ToPointer();
                int hash = ptr[0];
                for (int i = 1; i < obj.unmanagedSize; i++)
                {
                    hash = ((hash << 5) + hash) ^ ptr[i];
                }
                return hash ^ obj.unmanagedSize;
            }
            else
            {
                fixed (byte* ptr = obj.managed)
                {
                    int hash = ptr[0];
                    for (int i = 1; i < obj.managed.Length; i++)
                    {
                        hash = ((hash << 5) + hash) ^ ptr[i];
                    }
                    return hash ^ obj.managed.Length;
                }
            }
        }
    }

}
