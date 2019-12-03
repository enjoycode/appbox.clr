using System;
using System.Data.Common;

namespace appbox.Store
{
    /// <summary>
    /// 用于包装DbDataReader
    /// </summary>
    public struct SqlRowReader
    {
        private readonly DbDataReader _rawReader;

        public SqlRowReader(DbDataReader rawReader)
        {
            _rawReader = rawReader;
        }

        // public T GetFieldValue<T>(int ordinal)
        // {
        //     return _rawReader.GetFieldValue<T>(ordinal);
        // }

        public short? GetNullableInt16(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetInt16(ordinal);
        }

        public short GetInt16(int ordinal)
        {
            return _rawReader.GetInt16(ordinal);
        }

        public int? GetNullableInt32(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetInt32(ordinal);
        }

        public int GetInt32(int ordinal)
        {
            return _rawReader.GetInt32(ordinal);
        }

        public long? GetNullableInt64(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetInt64(ordinal);
        }

        public long GetInt64(int ordinal)
        {
            return _rawReader.GetInt64(ordinal);
        }

        public float? GetNullableFloat(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetFloat(ordinal);
        }

        public float GetFloat(int ordinal)
        {
            return _rawReader.GetFloat(ordinal);
        }

        public double? GetNullableDouble(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetDouble(ordinal);
        }

        public double GetDouble(int ordinal)
        {
            return _rawReader.GetDouble(ordinal);
        }

        public decimal? GetNullableDecimal(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetDecimal(ordinal);
        }

        public decimal GetDecimal(int ordinal)
        {
            return _rawReader.GetDecimal(ordinal);
        }

        public bool? GetNullableBoolean(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetBoolean(ordinal);
        }

        public bool GetBoolean(int ordinal)
        {
            return _rawReader.GetBoolean(ordinal);
        }

        public byte? GetNullableByte(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetByte(ordinal);
        }

        public byte GetByte(int ordinal)
        {
            return _rawReader.GetByte(ordinal);
        }

        public Guid? GetNullableGuid(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetGuid(ordinal);
        }

        public Guid GetGuid(int ordinal)
        {
            return _rawReader.GetGuid(ordinal);
        }

        public DateTime? GetNullableDateTime(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetDateTime(ordinal);
        }

        public DateTime GetDateTime(int ordinal)
        {
            return _rawReader.GetDateTime(ordinal);
        }

        public string GetString(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return _rawReader.GetString(ordinal);
        }

        public byte[] GetBinary(int ordinal)
        {
            if (_rawReader.IsDBNull(ordinal))
                return null;
            return (byte[])_rawReader.GetValue(ordinal);
        }
    }
}
