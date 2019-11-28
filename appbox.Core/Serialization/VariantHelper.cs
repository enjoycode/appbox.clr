using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace appbox.Serialization
{
    //Todo:暂全按小端字节序处理

    /// <summary>
    /// 可变长度整型值类型读写辅助类
    /// </summary>
    public static class VariantHelper
    {

        #region ====Write Methods====
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteInt32(Int32 value, Stream stream)
        {
            UInt32 num = (UInt32)((value << 1) ^ (value >> 0x1F));
            WriteUInt32(num, stream);
        }

        internal static void WriteUInt32(UInt32 value, Stream stream)
        {
            do
            {
                byte temp = (byte)((value & 0x7F) | 0x80);
                if ((value >>= 7) != 0)
                    stream.WriteByte(temp);
                else {
                    temp = (byte)(temp & 0x7F);
                    stream.WriteByte(temp);
                    break;
                }

            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteInt64(Int64 value, Stream stream)
        {
            UInt64 num = (UInt64)((value << 1) ^ (value >> 0x3F));
            WriteUInt64(num, stream);
        }

        internal static void WriteUInt64(UInt64 value, Stream stream)
        {
            do
            {
                byte temp = (byte)((value & ((UInt64)0x7FL)) | ((UInt64)0x80L));
                if ((value >>= 7) != 0L)
                    stream.WriteByte(temp);
                else {
                    temp = (byte)(temp & 0x7F);
                    stream.WriteByte(temp);
                    break;
                }
            } while (true);
        }

        #endregion

        #region ====Read Methods====

        internal static UInt32 ReadUInt32(Stream stream)
        {
            UInt32 data = (UInt32)stream.ReadByte();
            if ((data & 0x80) != 0)
            {
                data &= 0x7F;
                UInt32 num2 = (UInt32)stream.ReadByte();
                data |= (UInt32)((num2 & 0x7F) << 7);
                if ((num2 & 0x80) == 0)
                    return data;

                num2 = (UInt32)stream.ReadByte();
                data |= (UInt32)((num2 & 0x7F) << 14);
                if ((num2 & 0x80) == 0)
                    return data;

                num2 = (UInt32)stream.ReadByte();
                data |= (UInt32)((num2 & 0x7F) << 0x15);
                if ((num2 & 0x80) == 0)
                    return data;

                num2 = (UInt32)stream.ReadByte();
                data |= num2 << 0x1C;
                if ((num2 & 240) != 0)
                    throw new SerializationException(SerializationError.ReadVariantOutOfRange);
            }
            return data;
        }

        internal static UInt64 ReadUInt64(Stream stream)
        {
            UInt64 data = (UInt64)stream.ReadByte();
            if ((data & ((UInt64)0x80UL)) != 0UL)
            {
                data &= (UInt64)0x7fUL;
                UInt64 num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 7;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 14;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x15;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x1C;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x23;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x2a;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x31;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= (num2 & 0x7fUL) << 0x38;
                if ((num2 & ((UInt64)0x80UL)) == 0UL)
                    return data;
                num2 = (UInt64)stream.ReadByte();
                data |= num2 << 0x3f;
                if ((num2 & 18446744073709551614UL) != 0UL)
                    throw new SerializationException(SerializationError.ReadVariantOutOfRange);
            }
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Int32 ReadInt32(Stream stream)
        {
            Int32 temp = (Int32)ReadUInt32(stream);
            return -(temp & 1) ^ ((temp >> 1) & 0x7fffffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Int64 ReadInt64(Stream stream)
        {
            Int64 temp = (Int64)ReadUInt64(stream);
            return -(temp & 1L) ^ ((temp >> 1) & 0x7fffffffffffffffL);
        }

        #endregion

    }
}

