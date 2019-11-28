using System;
using System.Collections.Generic;
using appbox.Caching;
using System.Runtime.InteropServices;

namespace appbox.Data
{

    public sealed class KVTuple //TODO: 考虑结构体或使用缓存，另考虑直接移除
    {
        internal readonly List<KVField> fs = new List<KVField>(8);

        #region ====GetXXX Methods====
        //注意：所有值类型GetXXX()方法返回Nullable类型

        public string GetString(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetString();
            }
            return null;
        }

        public bool? GetBool(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetBool();
            }
            return null;
        }

        public byte[] GetBytes(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetBytes();
            }
            return null;
        }

        public int? GetInt32(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetInt32();
            }
            return null;
        }

        public long? GetInt64(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetInt64();
            }
            return null;
        }

        public ulong? GetUInt64(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetUInt64();
            }
            return null;
        }

        public float? GetFloat(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetFloat();
            }
            return null;
        }

        public DateTime? GetDateTime(ushort id)
        {
            var ticks = GetInt64(id);
            if (ticks.HasValue)
                return new DateTime(ticks.Value);
            return null;
        }

        public Guid? GetGuid(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetGuid();
            }
            return null;
        }

        public byte? GetByte(ushort id)
        {
            for (int i = 0; i < fs.Count; i++)
            {
                if (fs[i].Id == id)
                    return fs[i].GetByte();
            }
            return null;
        }
        #endregion

        #region ====Compare methods for db scan====
        //public bool StringEquals(ushort memberId, byte[] target)
        //{
        //    for (int i = 0; i < fs.Count; i++)
        //    {
        //        if (fs[i].Id == memberId)
        //        {
        //            if (fs[i].DataSize == 0) return target != null && target.Length == 0;
        //            unsafe
        //            {
        //                var span1 = new ReadOnlySpan<byte>(fs[i].DataPtr.ToPointer(), fs[i].DataSize);
        //                return span1.SequenceEqual(target);
        //            }
        //        }
        //    }
        //    return target == null;
        //}
        #endregion

        #region ====ReadFrom Methods====
        internal unsafe void ReadFrom(IntPtr valuePtr, int valueSize)
        {
            fs.Clear(); //reset for reuse
            byte* cur = (byte*)valuePtr.ToPointer();
            byte* end = cur + valueSize;
            while (cur < end)
            {
                var fi = new KVField();
                fi.ReadFrom(&cur);
                if (!fi.IsInvalid())
                {
                    fs.Add(fi);
                }
                cur += fi.DataSize;
            }
        }
        #endregion

        public override string ToString()
        {
            var sb = StringBuilderCache.Acquire();
            for (int i = 0; i < fs.Count; i++)
            {
                sb.Append(fs[i].Id);
                sb.Append(':');
                if (fs[i].DataSize != 0)
                {
                    sb.Append(StringHelper.ToHexString(fs[i].DataPtr, fs[i].DataSize));
                }
                else
                {
                    if (fs[i].DataPtr == IntPtr.Zero)
                        sb.Append("False");
                    else if (fs[i].DataPtr == new IntPtr(1))
                        sb.Append("True");
                    else
                        sb.Append("Null");
                }
                sb.Append('\t');
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

    }

}
