using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace appbox
{
    public static class StringHelper
    {
        const string alphabet = @"0123456789ABCDEF";

        /// <summary>
        /// 获取字符串HashCode，用以消除平台实现的差异性
        /// </summary>
        public static unsafe int GetHashCode(string value)
        {
            //fixed (char* str = value)
            //{
            //    char* chPtr = str;
            //    int num = 0x15051505;
            //    int num2 = num;
            //    int* numPtr = (int*)chPtr;
            //    for (int i = s.Length; i > 0; i -= 4)
            //    {
            //        num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
            //        if (i <= 2)
            //        {
            //            break;
            //        }
            //        num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
            //        numPtr += 2;
            //    }
            //    return (num + (num2 * 0x5d588b65));
            //}

            int hash1 = 5381;
            int hash2 = hash1;
            unsafe
            {
                fixed (char* src = value)
                {
                    int c;
                    char* s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
                }
            }

            return hash1 + (hash2 * 1566083941);
        }

        /// <summary>
        /// 将字符串以UTF-8编码方式写入流内
        /// </summary>
        public static unsafe void WriteTo(string s, Action<byte> writer)
        {
            fixed (char* chars = s)
            {
                int charIndex = 0;
                int surrogateChar = -1;
                int num = s.Length;
                while (charIndex < num)
                {
                    char c = chars[charIndex++];
                    if (surrogateChar > 0)
                    {
                        if (IsLowSurrogate(c))
                        {
                            surrogateChar = (surrogateChar - 0xd800) << 10;
                            surrogateChar += c - 0xdc00;
                            surrogateChar += 0x10000;
                            writer((byte)(240 | ((surrogateChar >> 0x12) & 7)));
                            writer((byte)(0x80 | ((surrogateChar >> 12) & 0x3f)));
                            writer((byte)(0x80 | ((surrogateChar >> 6) & 0x3f)));
                            writer((byte)(0x80 | (surrogateChar & 0x3f)));
                            surrogateChar = -1;
                        }
                        else if (IsHighSurrogate(c))
                        {
                            //if (this.isThrowException)
                            //{
                            //    throw new ArgumentException(null, "chars");
                            //}
                            EncodeThreeBytes(0xfffd, writer);
                            surrogateChar = c;
                        }
                        else
                        {
                            //if (this.isThrowException)
                            //{
                            //    throw new ArgumentException(null, "chars");
                            //}
                            EncodeThreeBytes(0xfffd, writer);
                            surrogateChar = -1;
                            charIndex--;
                        }
                    }
                    else if (c < '\x0080')
                    {
                        writer((byte)c);
                    }
                    else
                    {
                        if (c < 'ࠀ')
                        {
                            writer((byte)(0xc0 | ((c >> 6) & '\x001f')));
                            writer((byte)(0x80 | (c & '?')));
                            continue;
                        }
                        if (IsHighSurrogate(c))
                        {
                            surrogateChar = c;
                            continue;
                        }
                        if (IsLowSurrogate(c))
                        {
                            //if (this.isThrowException)
                            //{
                            //    throw new ArgumentException(null, "chars");
                            //}
                            EncodeThreeBytes(0xfffd, writer);
                            continue;
                        }
                        writer((byte)(0xe0 | ((c >> 12) & '\x000f')));
                        writer((byte)(0x80 | ((c >> 6) & '?')));
                        writer((byte)(0x80 | (c & '?')));
                    }
                }
                if (surrogateChar > 0)
                {
                    //if (this.isThrowException)
                    //{
                    //    throw new ArgumentException(null, "chars");
                    //}
                    EncodeThreeBytes(0xfffd, writer);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsLowSurrogate(char c)
        {
            return ((c >= 0xdc00) && (c <= 0xdfff));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsHighSurrogate(char c)
        {
            return ((c >= 0xd800) && (c <= 0xdbff));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeThreeBytes(int ch, Action<byte> writer)
        {
            writer((byte)(0xe0 | ((ch >> 12) & 15)));
            writer((byte)(0x80 | ((ch >> 6) & 0x3f)));
            writer((byte)(0x80 | (ch & 0x3f)));
        }

        /// <summary>
        /// 读取Stream内以UTF-8编码的字符串
        /// </summary>
        /// <returns>The from stream.</returns>
        /// <param name="charCount">Char count.</param>
        public static unsafe string ReadFrom(int charCount, Func<byte> reader)
        {
            char[] chars = new char[charCount];
            fixed (char* pChars = chars)
            {
                #region ---Fixed----
                int bits = 0;
                int trailCount = 0;
                bool isSurrogate = false;
                int byteSequence = 0;

                int charIndex = 0;
                //              int num5 = charIndex;
                bool readByte = true;

                while (charIndex < charCount)
                {
                    byte num6 = 0;
                    if (readByte)
                        num6 = reader();
                    else
                        readByte = true;

                    if (trailCount == 0)
                    {
                        if ((num6 & 0x80) == 0)
                        {
                            pChars[charIndex++] = (char)num6;
                            continue;
                        }
                        byte num7 = num6;
                        while ((num7 & 0x80) != 0)
                        {
                            num7 = (byte)(num7 << 1);
                            trailCount++;
                        }
                        switch (trailCount)
                        {
                            case 1:
                                trailCount = 0;
                                break;
                            case 2:
                                if ((num6 & 30) == 0)
                                {
                                    trailCount = 0;
                                }
                                break;
                            case 3:
                                byteSequence = 3;
                                break;
                            case 4:
                                byteSequence = 4;
                                isSurrogate = true;
                                break;
                            default:
                                trailCount = 0;
                                break;
                        }
                        if (trailCount == 0)
                        {
                            pChars[charIndex++] = (char)0xfffd;
                        }
                        else
                        {
                            bits = num7 >> trailCount;
                            trailCount--;
                        }
                        continue;
                    }
                    if ((num6 & 0xc0) != 0x80)
                    {
                        pChars[charIndex++] = (char)0xfffd;
                        readByte = false; //byteIndex--;
                        bits = 0;
                        trailCount = 0;
                        isSurrogate = false;
                        byteSequence = 0;
                        continue;
                    }
                    switch (byteSequence)
                    {
                        case 3:
                            if ((bits == 0) && ((num6 & 0x20) == 0))
                            {
                                break;
                            }
                            goto Label_01DC;

                        case 4:
                            if (bits == 0)
                            {
                                if ((num6 & 0x30) == 0)
                                {
                                    goto Label_023A;
                                }
                                goto Label_02A4;
                            }
                            goto Label_0261;

                        default:
                            goto Label_02A6;
                    }
                    pChars[charIndex++] = (char)0xfffd;
                    trailCount = 0;
                    byteSequence = 0;
                    continue;
                Label_01DC:
                    if (((bits == 13) && ((num6 & 0x20) != 0)))
                    {
                        pChars[charIndex++] = (char)0xfffd;
                        trailCount = 0;
                        byteSequence = 0;
                        continue;
                    }
                    byteSequence = 0;
                    goto Label_02A6;
                Label_023A:
                    pChars[charIndex++] = (char)0xfffd;
                    trailCount = 0;
                    isSurrogate = false;
                    byteSequence = 0;
                    continue;
                Label_0261:
                    if (((bits & 4) != 0) && (((bits & 3) != 0) || ((num6 & 0x30) != 0)))
                    {
                        pChars[charIndex++] = (char)0xfffd;
                        trailCount = 0;
                        isSurrogate = false;
                        byteSequence = 0;
                        continue;
                    }
                Label_02A4:
                    byteSequence = 0;
                Label_02A6:
                    if (--trailCount >= 0)
                    {
                        bits = (bits << 6) | (num6 & 0x3f);
                        if (trailCount != 0)
                        {
                            continue;
                        }
                        if (!isSurrogate)
                        {
                            pChars[charIndex++] = (char)bits;
                            continue;
                        }
                        pChars[charIndex++] = (char)(0xd7c0 + (bits >> 10));
                        pChars[charIndex++] = (char)(0xdc00 + (bits & 0x3ff));
                        isSurrogate = false;
                        byteSequence = 0;
                    }
                }
                if (((trailCount != 0)))
                {
                    pChars[charIndex++] = (char)0xfffd;
                }
                #endregion
            }
            return new string(chars);
        }

        [System.Diagnostics.Contracts.Pure]
        public static unsafe string ToHexString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            string result = new string(' ', checked(bytes.Length * 2));
            fixed (char* alphabetPtr = alphabet)
            fixed (char* resultPtr = result)
            {
                char* ptr = resultPtr;
                unchecked
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        *ptr++ = *(alphabetPtr + (bytes[i] >> 4));
                        *ptr++ = *(alphabetPtr + (bytes[i] & 0xF));
                    }
                }
            }
            return result;
        }

        public static unsafe string ToHexString(IntPtr bytePtr, int size)
        {
            if (bytePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(bytePtr));
            if (size <= 0)
                return null;

            byte* bytes = (byte*)bytePtr.ToPointer();

            string result = new string(' ', checked(size * 2));
            fixed (char* alphabetPtr = alphabet)
            fixed (char* resultPtr = result)
            {
                char* ptr = resultPtr;
                unchecked
                {
                    for (int i = 0; i < size; i++)
                    {
                        *ptr++ = *(alphabetPtr + (bytes[i] >> 4));
                        *ptr++ = *(alphabetPtr + (bytes[i] & 0xF));
                    }
                }
            }
            return result;
        }

        [System.Diagnostics.Contracts.Pure]
        public static unsafe byte[] FromHexString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            if (value.Length % 2 != 0)
                throw new ArgumentException("Hexadecimal value length must be even.", nameof(value));

            unchecked
            {
                byte[] result = new byte[value.Length / 2];
                fixed (char* valuePtr = value)
                {
                    char* valPtr = valuePtr;
                    for (int i = 0; i < result.Length; i++)
                    {
                        // 0(48) - 9(57) -> 0 - 9
                        // A(65) - F(70) -> 10 - 15
                        int b = *valPtr++; // High 4 bits.
                        int val = ((b - '0') + ((('9' - b) >> 31) & -7)) << 4;
                        b = *valPtr++; // Low 4 bits.
                        val += (b - '0') + ((('9' - b) >> 31) & -7);
                        result[i] = checked((byte)val);
                    }
                }
                return result;
            }
        }
    }
}

