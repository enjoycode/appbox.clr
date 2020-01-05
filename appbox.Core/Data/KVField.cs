#if FUTURE

using System;
using System.Runtime.InteropServices;

namespace appbox.Data
{

    public delegate bool KVFilterFunc(IntPtr vp, int vs, bool mvcc, ulong ts);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct VersionHead
    {
        internal ulong Timestamp;
        internal uint DataSize;
    }

    struct KVField
    {
        internal ushort Id;  //注意：已经从存储格式转换，如果等于ushort.MaxValue表示未找到
        internal int DataSize; //注意：已经转换为真实的大小, bool or Null = 0, 
        internal IntPtr DataPtr; //bool true = 1, false = 0, Null=2

        internal bool IsInvalid() => Id == ushort.MaxValue;

        internal static readonly KVField Invalid = new KVField { Id = ushort.MaxValue, DataSize = 0, DataPtr = IntPtr.Zero };

        internal unsafe void ReadFrom(byte** cur)
        {
            //reset first
            Id = ushort.MaxValue; //mean not found
            DataSize = 0;
            DataPtr = IntPtr.Zero;

            //读取Id及LenFlag
            ushort* idPtr = (ushort*)*cur;
            Id = (ushort)(*idPtr & IdUtil.MEMBERID_MASK); //由存储格式转换
            byte dataLenFlag = (byte)(*idPtr & IdUtil.MEMBERID_LENFLAG_MASK);
            *cur += 2;

            //读取数据指针
            switch (dataLenFlag)
            {
                case IdUtil.STORE_FIELD_VAR_FLAG:
                    DataSize = *((int*)*cur) & 0xFFFFFF;
                    *cur += 3;
                    DataPtr = new IntPtr(*cur);
                    break;
                case IdUtil.STORE_FIELD_BOOL_TRUE_FLAG:
                    DataPtr = new IntPtr(1);
                    DataSize = 0;
                    break;
                case IdUtil.STORE_FIELD_BOOL_FALSE_FLAG:
                    DataPtr = IntPtr.Zero;
                    DataSize = 0;
                    break;
                //case IdUtil.STORE_FIELD_ENTITYID_FLAG:
                case IdUtil.STORE_FIELD_16_LEN_FLAG:
                    DataPtr = new IntPtr(*cur);
                    DataSize = 16;
                    break;
                case IdUtil.STORE_FIELD_NULL_FLAG:
                    DataPtr = new IntPtr(2);
                    DataSize = 0;
                    break;
                default:
                    DataPtr = new IntPtr(*cur);
                    DataSize = dataLenFlag;
                    break;
            }
        }

        #region ====GetXXX Methods====
        public unsafe string GetString()
        {
            return DataSize == 0 ? string.Empty
                               : new string((sbyte*)DataPtr, 0, DataSize, System.Text.Encoding.UTF8);
        }

        public bool? GetBool()
        {
            if (DataSize != 0)
                throw new Exception("DataSize not match");
            if (DataPtr.ToInt32() == 2)
                return null;
            return DataPtr.ToInt32() == 1;
        }

        public byte[] GetBytes()
        {
            if (DataSize == 0)
                return null;

            byte[] res = new byte[DataSize];
            Marshal.Copy(DataPtr, res, 0, DataSize);
            return res;
        }

        public unsafe int? GetInt32()
        {
            if (DataSize == 4)
            {
                int* ptr = (int*)DataPtr.ToPointer();
                return *ptr;
            }
            throw new Exception("DataSize not match");
        }

        public unsafe long? GetInt64()
        {
            if (DataSize == 8)
            {
                long* ptr = (long*)DataPtr.ToPointer();
                return *ptr;
            }
            throw new Exception("DataSize not match");
        }

        public unsafe ulong? GetUInt64()
        {
            if (DataSize == 8)
            {
                ulong* ptr = (ulong*)DataPtr.ToPointer();
                return *ptr;
            }
            throw new Exception("DataSize not match");
        }

        public unsafe float? GetFloat()
        {
            if (DataSize == 4)
            {
                float* ptr = (float*)DataPtr.ToPointer();
                return *ptr;
            }
            throw new Exception("DataSize not match");
        }

        public DateTime? GetDateTime()
        {
            var ticks = GetInt64();
            if (ticks.HasValue)
                return new DateTime(ticks.Value);
            return null;
        }

        public unsafe Guid? GetGuid()
        {
            if (DataSize == 16)
            {
                Guid* ptr = (Guid*)DataPtr.ToPointer();
                return *ptr;
            }
            throw new Exception("DataSize not match");
        }

        public unsafe byte? GetByte()
        {
            if (DataSize == 1)
            {
                byte* ptr = (byte*)DataPtr.ToPointer();
                return ptr[0];
            }
            throw new Exception("DataSize not match");
        }
        #endregion
    }
}

#endif