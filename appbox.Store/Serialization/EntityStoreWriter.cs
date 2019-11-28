using System;
using System.Runtime.CompilerServices;
using appbox.Data;
using appbox.Models;
using appbox.Server;

namespace appbox.Store
{
    unsafe struct EntityStoreWriter
    {
        private readonly byte* dataPtr;
        private readonly IntPtr nativeStringPtr;
        private int index;
        //public int DataSize => index;

        #region ====Ctor====
        EntityStoreWriter(int size)
        {
            if (Runtime.RuntimeContext.Current.RuntimeId == 0)
            {
                nativeStringPtr = NativeApi.NewNativeString(size, out dataPtr);
            }
            else
            {
                //注意:在子进程创建的是NativeBytes
                nativeStringPtr = NativeBytes.MakeRaw(size);
                dataPtr = (byte*)(nativeStringPtr + 4).ToPointer();
            }
            index = 0;
        }

        internal unsafe EntityStoreWriter(byte* ptr, int offset)
        {
            nativeStringPtr = IntPtr.Zero;
            dataPtr = ptr;
            index = offset;
        }
        #endregion

        #region ====Write Methods====
        internal unsafe void WriteMember(ref EntityMember m, int* varSize, bool writeNull, bool orderDescFlag)
        {
            //如果分区键成员或索引键成员，需要写入排序标记
            var mid = m.Id;
            if (orderDescFlag)
                mid |= 1 << IdUtil.MEMBERID_ORDER_OFFSET;
            //根据成员类型按存储格式写入
            if (m.MemberType == EntityMemberType.DataField)
            {
                if (m.HasValue)
                {
                    //TODO: fix others
                    switch (m.ValueType)
                    {
                        case EntityFieldType.String:
                            WriteUInt16((ushort)(mid | IdUtil.STORE_FIELD_VAR_FLAG));
                            WriteString((string)m.ObjectValue, varSize);
                            break;
                        case EntityFieldType.Binary:
                            WriteUInt16((ushort)(mid | IdUtil.STORE_FIELD_VAR_FLAG));
                            WriteBytes((byte[])m.ObjectValue, varSize);
                            break;
                        case EntityFieldType.UInt16:
                            WriteUInt16((ushort)(mid | 2));
                            WriteUInt16(m.UInt16Value);
                            break;
                        case EntityFieldType.Int32:
                            WriteUInt16((ushort)(mid | 4));
                            WriteInt32(m.Int32Value);
                            break;
                        case EntityFieldType.UInt64:
                            WriteUInt16((ushort)(mid | 8));
                            WriteUInt64(m.UInt64Value);
                            break;
                        case EntityFieldType.Guid:
                            WriteUInt16((ushort)(mid | IdUtil.STORE_FIELD_16_LEN_FLAG));
                            WriteGuid(m.GuidValue);
                            break;
                        case EntityFieldType.EntityId:
                            WriteUInt16((ushort)(mid | IdUtil.STORE_FIELD_16_LEN_FLAG));
                            WriteGuid((EntityId)m.ObjectValue);
                            break;
                        case EntityFieldType.Byte:
                            WriteUInt16((ushort)(mid | 1));
                            dataPtr[index++] = m.ByteValue;
                            break;
                        case EntityFieldType.DateTime:
                            WriteUInt16((ushort)(mid | 8));
                            WriteInt64(m.DateTimeValue.Ticks);
                            break;
                        case EntityFieldType.Boolean:
                            var flag = m.BooleanValue ? IdUtil.STORE_FIELD_BOOL_TRUE_FLAG : IdUtil.STORE_FIELD_BOOL_FALSE_FLAG;
                            WriteUInt16((ushort)(mid | flag));
                            break;
                        case EntityFieldType.Float:
                            WriteUInt16((ushort)(mid | 4));
                            WriteFloat(m.FloatValue);
                            break;
                        case EntityFieldType.Double:
                            WriteUInt16((ushort)(mid | 8));
                            WriteDouble(m.DoubleValue);
                            break;
                        default:
                            throw ExceptionHelper.NotImplemented($"{m.ValueType}");
                    }
                }
                else
                {
                    if (writeNull)
                    {
                        WriteUInt16((ushort)(mid | IdUtil.STORE_FIELD_NULL_FLAG));
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteVarSize(int* size)
        {
            int* ptr = (int*)(dataPtr + index);
            *ptr = *size;
            index += 3; //注意只有3字节
        }

        private unsafe void WriteBytes(byte[] data, int* varSize)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            WriteVarSize(varSize);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, new IntPtr(dataPtr + index), data.Length);
            index += data.Length;
        }

        /// <summary>
        /// Writes the string's utf8 data
        /// </summary>
        /// <param name="varSize">null不写3字节长度头</param>
        internal unsafe void WriteString(string s, int* varSize)
        {
            if (varSize != null)
            {
                WriteVarSize(varSize);
            }

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
                        if (StringHelper.IsLowSurrogate(c))
                        {
                            surrogateChar = (surrogateChar - 0xd800) << 10;
                            surrogateChar += c - 0xdc00;
                            surrogateChar += 0x10000;
                            dataPtr[index++] = (byte)(240 | ((surrogateChar >> 0x12) & 7));
                            dataPtr[index++] = (byte)(0x80 | ((surrogateChar >> 12) & 0x3f));
                            dataPtr[index++] = (byte)(0x80 | ((surrogateChar >> 6) & 0x3f));
                            dataPtr[index++] = (byte)(0x80 | (surrogateChar & 0x3f));
                            surrogateChar = -1;
                        }
                        else if (StringHelper.IsHighSurrogate(c))
                        {
                            EncodeThreeBytes(0xfffd);
                            surrogateChar = c;
                        }
                        else
                        {
                            EncodeThreeBytes(0xfffd);
                            surrogateChar = -1;
                            charIndex--;
                        }
                    }
                    else if (c < '\x0080')
                    {
                        dataPtr[index++] = (byte)c;
                    }
                    else
                    {
                        if (c < 'ࠀ')
                        {
                            dataPtr[index++] = (byte)(0xc0 | ((c >> 6) & '\x001f'));
                            dataPtr[index++] = (byte)(0x80 | (c & '?'));
                            continue;
                        }
                        if (StringHelper.IsHighSurrogate(c))
                        {
                            surrogateChar = c;
                            continue;
                        }
                        if (StringHelper.IsLowSurrogate(c))
                        {
                            EncodeThreeBytes(0xfffd);
                            continue;
                        }
                        dataPtr[index++] = (byte)(0xe0 | ((c >> 12) & '\x000f'));
                        dataPtr[index++] = (byte)(0x80 | ((c >> 6) & '?'));
                        dataPtr[index++] = (byte)(0x80 | (c & '?'));
                    }
                }
                if (surrogateChar > 0)
                {
                    EncodeThreeBytes(0xfffd);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncodeThreeBytes(int ch)
        {
            dataPtr[index++] = (byte)(0xe0 | ((ch >> 12) & 15));
            dataPtr[index++] = (byte)(0x80 | ((ch >> 6) & 0x3f));
            dataPtr[index++] = (byte)(0x80 | (ch & 0x3f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void WriteUInt16(ushort value)
        {
            ushort* ptr = (ushort*)(dataPtr + index);
            *ptr = value;
            index += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteUInt32(uint value)
        {
            uint* ptr = (uint*)(dataPtr + index);
            *ptr = value;
            index += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void WriteInt32(int value)
        {
            int* ptr = (int*)(dataPtr + index);
            *ptr = value;
            index += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteFloat(float value)
        {
            float* ptr = (float*)(dataPtr + index);
            *ptr = value;
            index += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteDouble(double value)
        {
            double* ptr = (double*)(dataPtr + index);
            *ptr = value;
            index += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteInt64(long value)
        {
            long* ptr = (long*)(dataPtr + index);
            *ptr = value;
            index += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteUInt64(ulong value)
        {
            ulong* ptr = (ulong*)(dataPtr + index);
            *ptr = value;
            index += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteGuid(Guid value)
        {
            Guid* ptr = (Guid*)(dataPtr + index);
            *ptr = value;
            index += 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteSpan(ReadOnlySpan<byte> span)
        {
            var destSpan = new Span<byte>(dataPtr + index, span.Length);
            span.CopyTo(destSpan);
            index += span.Length;
        }


        internal unsafe void WriteIndexKey(Entity entity, EntityIndexModel indexModel, int* varSizes)
        {
            var id = entity.Id.Data;
            byte* idptr = (byte*)&id;
            //写入EntityId's RaftGroupId
            WriteSpan(new ReadOnlySpan<byte>(idptr, 6));
            //写入IndexId
            dataPtr[index++] = indexModel.IndexId;
            //写入各成员
            for (int i = 0; i < indexModel.Fields.Length; i++)
            {
                //注意MemberId写入排序标记
                WriteMember(ref entity.GetMember(indexModel.Fields[i].MemberId), varSizes + i, true,
                    indexModel.Fields[i].OrderByDesc);
            }
            //非惟一索引写入EntityId的第二部分
            if (!indexModel.Unique)
            {
                WriteSpan(new ReadOnlySpan<byte>(idptr + 6, 10));
            }
        }
        #endregion

        #region ====Static Methods====
        /// <summary>
        /// 计算分区键的大小
        /// </summary>
        internal unsafe static int CalcPartitionKeysSize(Entity entity, EntityModel model, int* varSizes)
        {
            var totalSize = 0;
            for (int i = 0; i < model.SysStoreOptions.PartitionKeys.Length; i++)
            {
                switch (model.SysStoreOptions.PartitionKeys[i].Rule)
                {
                    case PartitionKeyRule.Hash:
                    case PartitionKeyRule.RangeOfDate:
                        totalSize += 6; //暂统一2 + 4字节
                        break;
                    default:
                        totalSize += CalcMemberSize(ref entity.GetMember(model.SysStoreOptions.PartitionKeys[i].MemberId),
                                                varSizes + i, true); //TODO: check write null
                        break;
                }
            }
            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetHashOfPK(object fieldValue, int hashNum)
        {
            int hashCode = fieldValue == null ? 0 : fieldValue.GetHashCode(); //TODO:better hash
            return hashCode % hashNum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetRangeOfPK(DateTime fieldValue, DatePeriod period)
        {
            int pkValue = fieldValue.Year << 16;
            switch (period)
            {
                case DatePeriod.Month:
                    pkValue |= fieldValue.Month;
                    break;
                case DatePeriod.Day:
                    pkValue |= fieldValue.DayOfYear;
                    break;
            }
            return pkValue;
        }

        internal unsafe static void WritePartitionKeys(Entity entity, EntityModel model, byte* pkPtr, int* varSizes)
        {
            var w = new EntityStoreWriter(pkPtr, 5);
            ushort memberId;
            for (int i = 0; i < model.SysStoreOptions.PartitionKeys.Length; i++)
            {
                memberId = model.SysStoreOptions.PartitionKeys[i].MemberId;
                switch (model.SysStoreOptions.PartitionKeys[i].Rule)
                {
                    case PartitionKeyRule.Hash:
                        {
                            object fieldValue = memberId == 0 ? entity.CreateTimeUtc : entity.GetMember(memberId).BoxedValue;
                            int pkValue = GetHashOfPK(fieldValue, model.SysStoreOptions.PartitionKeys[i].RuleArgument);
                            w.WriteUInt16((ushort)(memberId | 4)); //不需要写入排序标记
                            w.WriteInt32(pkValue);
                        }
                        break;
                    case PartitionKeyRule.RangeOfDate:
                        {
                            DateTime fieldValue = memberId == 0 ? entity.CreateTimeUtc : entity.GetDateTime(memberId);
                            int pkValue = GetRangeOfPK(fieldValue, (DatePeriod)model.SysStoreOptions.PartitionKeys[i].RuleArgument);
                            if (model.SysStoreOptions.PartitionKeys[i].OrderByDesc) //需要写入排序标记
                                memberId |= 1 << IdUtil.MEMBERID_ORDER_OFFSET;
                            w.WriteUInt16((ushort)(memberId | 4)); 
                            w.WriteInt32(pkValue);
                        }
                        break;
                    default:
                        w.WriteMember(ref entity.GetMember(memberId),varSizes + i, true, //TODO: check write null
                            model.SysStoreOptions.PartitionKeys[i].OrderByDesc); 
                        break;
                }
            }
        }

        internal unsafe static int CalcMemberSize(ref EntityMember m, int* varSize, bool withNull)
        {
            if (m.MemberType == EntityMemberType.DataField)
            {
                if (m.HasValue)
                {
                    switch (m.ValueType)
                    {
                        //TODO: others
                        case EntityFieldType.EntityId:
                        case EntityFieldType.Guid:
                        case EntityFieldType.Decimal: return 2 + 16;

                        case EntityFieldType.Boolean: return 2; //注意: bool值存储在标记位内，参考编码规则

                        case EntityFieldType.Byte: return 2 + 1;

                        case EntityFieldType.DateTime:
                        case EntityFieldType.Int64:
                        case EntityFieldType.UInt64:
                        case EntityFieldType.Double: return 2 + 8;

                        case EntityFieldType.Enum:
                        case EntityFieldType.Float:
                        case EntityFieldType.UInt32:
                        case EntityFieldType.Int32: return 2 + 4;

                        case EntityFieldType.Int16:
                        case EntityFieldType.UInt16: return 2 + 2;

                        case EntityFieldType.Binary:
                            {
                                *varSize = ((byte[])m.ObjectValue).Length;
                                return 2 + 3 + *varSize;
                            }
                        case EntityFieldType.String:
                            {
                                *varSize = CalcStringUtf8Size((string)m.ObjectValue);
                                return 2 + 3 + *varSize;
                            }
                        default: return 0;
                    }
                }
                else
                {
                    if (withNull)
                    {
                        return 2;
                    }
                }
            }
            return 0;
        }

        internal static int CalcStringUtf8Size(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            //TODO:优化
            int size = 0;
            StringHelper.WriteTo(value, b => size++);
            return size;
        }

        /// <summary>
        /// 将Entity转换为Value部分，返回非托管的内存指针
        /// </summary>
        /// <returns>主进程NativeString; 子进程NativeBytes</returns>
        internal unsafe static IntPtr WriteEntity(Entity entity, out int dataSize)
        {
            int* varSizes = stackalloc int[entity.Members.Length]; //主要用于记录String utf8数据长度,避免重复计算
            int totalSize = 0;
            for (int i = 0; i < entity.Members.Length; i++)
            {
                totalSize += CalcMemberSize(ref entity.Members[i], varSizes + i, false);
            }
            dataSize = totalSize;

            var w = new EntityStoreWriter(totalSize);
            for (int i = 0; i < entity.Members.Length; i++)
            {
                w.WriteMember(ref entity.Members[i], varSizes + i, false, false);
            }

            return w.nativeStringPtr;
        }

        /// <summary>
        /// 写索引Value
        /// </summary>
        /// <returns>非惟一索引且没有覆盖字段返回IntPtr.Zero</returns>
        internal unsafe static IntPtr WriteIndexData(Entity entity, EntityIndexModel indexModel, out int dataSize)
        {
            if (!(indexModel.Unique || indexModel.HasStoringFields))
            {
                dataSize = 0;
                return IntPtr.Zero;
            }

            int* varSizes = null;
            int totalSize = indexModel.Unique ? 2 + 16 : 0;
            if (indexModel.HasStoringFields)
            {
                int* vars = stackalloc int[indexModel.StoringFields.Length];
                for (int i = 0; i < indexModel.StoringFields.Length; i++)
                {
                    totalSize += CalcMemberSize(ref entity.GetMember(indexModel.StoringFields[i]), vars + i, true);
                }
                varSizes = vars;
            }
            dataSize = totalSize;

            var w = new EntityStoreWriter(totalSize);
            //先写入惟一索引指向的EntityId
            if (indexModel.Unique)
            {
                w.WriteUInt16(IdUtil.STORE_FIELD_ID_OF_ENTITY_ID);
                w.WriteGuid(entity.Id.Data);
            }
            //再写入StoringFields  
            if (indexModel.HasStoringFields)
            {
                for (int i = 0; i < indexModel.StoringFields.Length; i++)
                {
                    w.WriteMember(ref entity.GetMember(indexModel.StoringFields[i]), varSizes + i, true, false); //TODO:考虑不写入Null
                }
            }

            return w.nativeStringPtr;
        }
        #endregion
    }
}
