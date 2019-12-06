using System;
using System.Runtime.InteropServices;
using appbox.Serialization;

namespace appbox.Data
{
    /// <summary>
    /// 任意值，可包含C#常规则内置类型或引用类型或Boxed的结构体
    /// 主要目的是减少常规类型的装箱操作
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct AnyValue
    {
        #region ====内存结构===
        [FieldOffset(0)]
        internal Guid GuidValue;
        [FieldOffset(0)]
        internal ushort UInt16Value;
        [FieldOffset(0)]
        internal short Int16Value;
        [FieldOffset(0)]
        internal int Int32Value;
        [FieldOffset(0)]
        internal uint UInt32Value;
        [FieldOffset(0)]
        internal long Int64Value;
        [FieldOffset(0)]
        internal ulong UInt64Value;
        [FieldOffset(0)]
        internal byte ByteValue;
        [FieldOffset(0)]
        internal bool BooleanValue;
        [FieldOffset(0)]
        internal DateTime DateTimeValue;
        [FieldOffset(0)]
        internal float FloatValue;
        [FieldOffset(0)]
        internal double DoubleValue;
        [FieldOffset(0)]
        internal decimal DecimalValue;

        [FieldOffset(16)]
        internal object ObjectValue;

        [FieldOffset(24)]
        internal AnyValueType Type;
        #endregion

        #region ====readonly Property====
        public object BoxedValue
        {
            get
            {
                return Type switch
                {
                    AnyValueType.Boolean => BooleanValue,
                    AnyValueType.Byte => ByteValue,
                    AnyValueType.Int16 => Int16Value,
                    AnyValueType.UInt16 => UInt16Value,
                    AnyValueType.Int32 => Int32Value,
                    AnyValueType.UInt32 => UInt32Value,
                    AnyValueType.Int64 => Int64Value,
                    AnyValueType.UInt64 => UInt64Value,
                    AnyValueType.Float => FloatValue,
                    AnyValueType.Double => DoubleValue,
                    AnyValueType.DateTime => DateTimeValue,
                    AnyValueType.Decimal => DecimalValue,
                    AnyValueType.Guid => GuidValue,
                    _ => ObjectValue,
                };
            }
        }
        #endregion

        #region ====FromXXX Methods, 仅用于生成虚拟服务代码的IService接口====
        public static AnyValue From(int v)
        {
            return new AnyValue { Int32Value = v, Type = AnyValueType.Int32 };
        }

        public static AnyValue From(object v)
        {
            return new AnyValue { ObjectValue = v, Type = AnyValueType.Object };
        }
        #endregion

        #region ====隐式转换，仅用于方便服务端编码====
        //注意隐式转换不支持接口类型及object
        public static implicit operator AnyValue(uint v)
        {
            return new AnyValue { UInt32Value = v, Type = AnyValueType.UInt32 };
        }

        public static implicit operator AnyValue(int v)
        {
            return new AnyValue { Int32Value = v, Type = AnyValueType.Int32 };
        }

        public static implicit operator AnyValue(string v)
        {
            return new AnyValue { ObjectValue = v, Type = AnyValueType.Object };
        }

        public static implicit operator AnyValue(Entity obj)
        {
            return new AnyValue { ObjectValue = obj, Type = AnyValueType.Object };
        }
        #endregion

        #region ====Serialization====
        internal void WriteObject(BinSerializer bs)
        {
            bs.Write((byte)Type);
            switch (Type)
            {
                case AnyValueType.Boolean: bs.Write(BooleanValue); break;
                case AnyValueType.Byte: bs.Write(ByteValue); break;
                case AnyValueType.Int16: bs.Write(Int16Value); break;
                case AnyValueType.UInt16: bs.Write(UInt16Value); break;
                case AnyValueType.Int32: bs.Write(Int32Value); break;
                case AnyValueType.UInt32: bs.Write(UInt32Value); break;
                case AnyValueType.Int64: bs.Write(Int64Value); break;
                case AnyValueType.UInt64: bs.Write(UInt64Value); break;
                case AnyValueType.Float: bs.Write(FloatValue); break;
                case AnyValueType.Double: bs.Write(DoubleValue); break;
                case AnyValueType.DateTime: bs.Write(DateTimeValue); break;
                case AnyValueType.Decimal: bs.Write(DecimalValue); break;
                case AnyValueType.Guid: bs.Write(GuidValue); break;
                case AnyValueType.Object: bs.Serialize(ObjectValue); break;
            }
        }

        internal void ReadObject(BinSerializer bs)
        {
            Type = (AnyValueType)bs.ReadByte();
            switch (Type)
            {
                case AnyValueType.Boolean: BooleanValue = bs.ReadBoolean(); break;
                case AnyValueType.Byte: ByteValue = bs.ReadByte(); break;
                case AnyValueType.Int16: Int16Value = bs.ReadInt16(); break;
                case AnyValueType.UInt16: UInt16Value = bs.ReadUInt16(); break;
                case AnyValueType.Int32: Int32Value = bs.ReadInt32(); break;
                case AnyValueType.UInt32: UInt32Value = bs.ReadUInt32(); break;
                case AnyValueType.Int64: Int64Value = bs.ReadInt64(); break;
                case AnyValueType.UInt64: UInt64Value = bs.ReadUInt64(); break;
                case AnyValueType.Float: FloatValue = bs.ReadFloat(); break;
                case AnyValueType.Double: DoubleValue = bs.ReadDouble(); break;
                case AnyValueType.DateTime: DateTimeValue = bs.ReadDateTime(); break;
                case AnyValueType.Decimal: DecimalValue = bs.ReadDecimal(); break;
                case AnyValueType.Guid: GuidValue = bs.ReadGuid(); break;
                case AnyValueType.Object: ObjectValue = bs.Deserialize(); break;
            }
        }
        #endregion
    }

    enum AnyValueType : byte
    {
        Object,
        Boolean,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        DateTime,
        Decimal,
        Guid,
    }
}
