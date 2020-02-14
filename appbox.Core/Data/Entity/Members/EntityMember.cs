using System;
using System.Runtime.InteropServices;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Data
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EntityMember
    {
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
        internal ushort Id;

        //TODO:合并以下标记
        [FieldOffset(26)]
        internal EntityMemberFlag Flag;
        [FieldOffset(27)]
        internal EntityMemberType MemberType;
        [FieldOffset(28)]
        internal EntityFieldType ValueType;
        //TODO:补齐

        public bool HasChanged
        {
            get
            {
                //if (MemberType == EntityMemberType.Formula || MemberType == EntityMemberType.Aggregate)
                //    throw new NotImplementedException();
                //else
                return Flag.HasChanged;
            }
        }

        public bool HasValue
        {
            get
            {
                switch (MemberType)
                {
                    case EntityMemberType.EntityRef:
                    case EntityMemberType.EntitySet:
                        return Flag.HasLoad && ObjectValue != null;
                    case EntityMemberType.EntityRefDisplayText:
                        //case EntityMemberType.ImageRef:
                        return ObjectValue != null;
                    default:
                        return Flag.HasValue;
                }
            }
        }

        public object BoxedValue
        {
            get
            {
                if (MemberType == EntityMemberType.DataField)
                {
                    switch (ValueType) //TODO:其他值类型
                    {
                        case EntityFieldType.Binary: return ObjectValue;
                        case EntityFieldType.Boolean: return BooleanValue;
                        case EntityFieldType.Byte: return ByteValue;
                        case EntityFieldType.DateTime: return DateTimeValue;
                        case EntityFieldType.Decimal: return DecimalValue;
                        case EntityFieldType.Double: return DoubleValue;
                        case EntityFieldType.Enum: return Int32Value;
                        case EntityFieldType.Float: return FloatValue;
                        case EntityFieldType.Guid: return GuidValue;
                        case EntityFieldType.Int16: return Int16Value;
                        case EntityFieldType.UInt16: return UInt16Value;
                        case EntityFieldType.Int32: return Int32Value;
                        case EntityFieldType.UInt32: return UInt32Value;
                        case EntityFieldType.Int64: return Int64Value;
                        case EntityFieldType.UInt64: return UInt64Value;
                    }
                }
                return ObjectValue;
            }
        }

        public static bool IsValueSame(ref EntityMember a, ref EntityMember b)
        {
            return a.GuidValue == b.GuidValue && a.ObjectValue == b.ObjectValue;
        }

        #region ====隐式转换,只转换值不包含MemberId====
        // 这样做是为了方便设计时将值隐式转换为相应的EntityMember, 只要用于虚拟Entity的构造及Load参数
        public static implicit operator EntityMember(string v)
        {
            var r = new EntityMember() { ObjectValue = v, ValueType = EntityFieldType.String };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(float v)
        {
            var r = new EntityMember() { FloatValue = v, ValueType = EntityFieldType.Float };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(double v)
        {
            var r = new EntityMember() { DoubleValue = v, ValueType = EntityFieldType.Double };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(DateTime v)
        {
            var r = new EntityMember() { DateTimeValue = v, ValueType = EntityFieldType.DateTime };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(long v)
        {
            var r = new EntityMember() { Int64Value = v, ValueType = EntityFieldType.Int64 };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(uint v)
        {
            var r = new EntityMember() { UInt32Value = v, ValueType = EntityFieldType.UInt32 };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(int v)
        {
            var r = new EntityMember() { Int32Value = v, ValueType = EntityFieldType.Int32 };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(short v)
        {
            var r = new EntityMember() { Int16Value = v, ValueType = EntityFieldType.Int16 };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(ushort v)
        {
            var r = new EntityMember() { UInt16Value = v, ValueType = EntityFieldType.UInt16 };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(byte v)
        {
            var r = new EntityMember() { ByteValue = v, ValueType = EntityFieldType.Byte };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(Guid v)
        {
            var r = new EntityMember() { GuidValue = v, ValueType = EntityFieldType.Guid };
            r.Flag.HasValue = true;
            return r;
        }

        public static implicit operator EntityMember(decimal v)
        {
            var r = new EntityMember() { DecimalValue = v, ValueType = EntityFieldType.Decimal };
            r.Flag.HasValue = true;
            return r;
        }
        #endregion

        #region ====Serialization====
        internal void Write(BinSerializer bs)
        {
            bs.Write(Id);
            bs.Write((byte)MemberType);
            bs.Write((byte)ValueType);
            bs.Write(Flag.Data);

            switch (MemberType)
            {
                case EntityMemberType.DataField:
                    switch (ValueType)
                    {
                        case EntityFieldType.Boolean: bs.Write(BooleanValue); break;
                        case EntityFieldType.Byte: bs.Write(ByteValue); break;
                        case EntityFieldType.Float: bs.Write(FloatValue); break;
                        case EntityFieldType.Double: bs.Write(DoubleValue); break;
                        case EntityFieldType.Enum: bs.Write(Int32Value); break;
                        case EntityFieldType.Int16: bs.Write(Int16Value); break;
                        case EntityFieldType.UInt16: bs.Write(UInt16Value); break;
                        case EntityFieldType.Int32: bs.Write(Int32Value); break;
                        case EntityFieldType.UInt32: bs.Write(UInt32Value); break;
                        case EntityFieldType.Int64: bs.Write(Int64Value); break;
                        case EntityFieldType.UInt64: bs.Write(UInt64Value); break;
                        case EntityFieldType.DateTime: bs.Write(DateTimeValue); break;
                        case EntityFieldType.Guid: bs.Write(GuidValue); break;
                        case EntityFieldType.Decimal: bs.Write(DecimalValue); break;
                        case EntityFieldType.String: bs.Write(ObjectValue as string); break;
                        case EntityFieldType.Binary: bs.Write(ObjectValue as byte[]); break;
                        default: throw new NotImplementedException($"Id:{Id} ValueType:{ValueType}");
                    }
                    break;
                case EntityMemberType.EntityRef:
                case EntityMemberType.EntitySet: bs.Serialize(ObjectValue); break;
                //case EntityMemberType.ImageRef: bs.Serialize(this.ObjectValue); break;
                //case EntityMemberType.FieldSet:
                //if (this.ObjectValue == null)
                //{
                //    bs.Write(-1);
                //}
                //else
                //{
                //    switch (this.ValueType)
                //    {
                //        case EntityFieldType.String:
                //            {
                //                var hashSet = (HashSet<string>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Integer:
                //            {
                //                var hashSet = (HashSet<int>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Guid:
                //            {
                //                var hashSet = (HashSet<Guid>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Float:
                //            {
                //                var hashSet = (HashSet<float>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Double:
                //            {
                //                var hashSet = (HashSet<double>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Decimal:
                //            {
                //                var hashSet = (HashSet<Decimal>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.DateTime:
                //            {
                //                var hashSet = (HashSet<DateTime>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        case EntityFieldType.Byte:
                //            {
                //                var hashSet = (HashSet<byte>)this.ObjectValue;
                //                bs.Write(hashSet.ToArray());
                //            }
                //            break;
                //        default: throw new NotSupportedException($"不支持二进制序列化FieldSet: {ValueType}");
                //    }
                //}
                //break;
                default:
                    bs.Write(GuidValue);
                    bs.Serialize(ObjectValue);
                    break;
            }
        }

        internal void Read(BinSerializer bs)
        {
            Id = bs.ReadUInt16();
            MemberType = (EntityMemberType)bs.ReadByte();
            ValueType = (EntityFieldType)bs.ReadByte();
            Flag.Data = bs.ReadByte();

            switch (MemberType)
            {
                case EntityMemberType.DataField:
                    switch (ValueType)
                    {
                        case EntityFieldType.Boolean: BooleanValue = bs.ReadBoolean(); break;
                        case EntityFieldType.Byte: ByteValue = bs.ReadByte(); break;
                        case EntityFieldType.Float: FloatValue = bs.ReadFloat(); break;
                        case EntityFieldType.Double: DoubleValue = bs.ReadDouble(); break;
                        case EntityFieldType.Enum: Int32Value = bs.ReadInt32(); break;
                        case EntityFieldType.Int16: Int16Value = bs.ReadInt16(); break;
                        case EntityFieldType.UInt16: UInt16Value = bs.ReadUInt16(); break;
                        case EntityFieldType.Int32: Int32Value = bs.ReadInt32(); break;
                        case EntityFieldType.UInt32: UInt32Value = bs.ReadUInt32(); break;
                        case EntityFieldType.Int64: Int64Value = bs.ReadInt64(); break;
                        case EntityFieldType.UInt64: UInt64Value = bs.ReadUInt64(); break;
                        case EntityFieldType.DateTime: DateTimeValue = bs.ReadDateTime(); break;
                        case EntityFieldType.Guid: GuidValue = bs.ReadGuid(); break;
                        case EntityFieldType.Decimal: DecimalValue = bs.ReadDecimal(); break;
                        case EntityFieldType.String: ObjectValue = bs.ReadString(); break;
                        case EntityFieldType.Binary: ObjectValue = bs.ReadByteArray(); break;
                        default: throw new NotSupportedException();
                    }
                    break;
                case EntityMemberType.EntityRef:
                case EntityMemberType.EntitySet: ObjectValue = bs.Deserialize(); break;
                //case EntityMemberType.ImageRef: this.ObjectValue = bs.Deserialize(); break;
                //case EntityMemberType.FieldSet:
                //switch (this.ValueType)
                //{
                //    case EntityFieldType.String:
                //        {
                //            var array = bs.ReadStringArray();
                //            this.ObjectValue = array == null ? null : new HashSet<string>(array);
                //        }
                //        break;
                //    case EntityFieldType.Integer:
                //        {
                //            var array = bs.ReadInt32Array();
                //            this.ObjectValue = array == null ? null : new HashSet<int>(array);
                //        }
                //        break;
                //    case EntityFieldType.Guid:
                //        {
                //            var array = bs.ReadGuidArray();
                //            this.ObjectValue = array == null ? null : new HashSet<Guid>(array);
                //        }
                //        break;
                //    case EntityFieldType.Float:
                //        {
                //            var array = bs.ReadFloatArray();
                //            this.ObjectValue = array == null ? null : new HashSet<float>(array);
                //        }
                //        break;
                //    case EntityFieldType.Double:
                //        {
                //            var array = bs.ReadDoubleArray();
                //            this.ObjectValue = array == null ? null : new HashSet<double>(array);
                //        }
                //        break;
                //    case EntityFieldType.Decimal:
                //        {
                //            var array = bs.ReadDecimalArray();
                //            this.ObjectValue = array == null ? null : new HashSet<Decimal>(array);
                //        }
                //        break;
                //    case EntityFieldType.DateTime:
                //        {
                //            var array = bs.ReadDateTimeArray();
                //            this.ObjectValue = array == null ? null : new HashSet<DateTime>(array);
                //        }
                //        break;
                //    case EntityFieldType.Byte:
                //        {
                //            var array = bs.ReadByteArray();
                //            this.ObjectValue = array == null ? null : new HashSet<byte>(array);
                //        }
                //        break;
                //    default: throw new NotSupportedException($"不支持二进制反序列化FieldSet: {ValueType}");
                //}
                //break;
                default:
                    GuidValue = bs.ReadGuid();
                    ObjectValue = bs.Deserialize();
                    break;
            }
        }
        #endregion
    }
}
