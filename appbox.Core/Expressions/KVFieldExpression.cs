using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Expressions
{
    /// <summary>
    /// 用于ClrScan的过滤条件表达式，表示KV字段
    /// </summary>
    public sealed class KVFieldExpression : Expression
    {

        public override ExpressionType Type => ExpressionType.KVFieldExpression;
        public ushort FieldId { get; private set; }
        public EntityFieldType FieldType { get; private set; }

        #region ====Ctor====
        internal KVFieldExpression() { }

        public KVFieldExpression(ushort id, EntityFieldType type)
        {
            FieldId = id;
            FieldType = type;
        }
        #endregion

        #region ====Overrides====
        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            var vp = ctx.GetParameter("vp");
            var vs = ctx.GetParameter("vs");
            var mv = ctx.GetParameter("mv");
            var ts = ctx.GetParameter("ts");

            MethodInfo methodInfo;
            switch (FieldType)
            {
                case EntityFieldType.String:
                    methodInfo = GetStringMemberInfo; break;
                case EntityFieldType.Int32:
                    methodInfo = GetInt32MemberInfo; break;
                case EntityFieldType.Guid:
                    methodInfo = GetGuidMemberInfo; break;
                case EntityFieldType.Byte:
                    methodInfo = GetByteMemberInfo; break;
                default:
                    throw ExceptionHelper.NotImplemented();
            }
            var id = System.Linq.Expressions.Expression.Constant(FieldId);
            return System.Linq.Expressions.Expression.Call(methodInfo, id, vp, vs, mv, ts);
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            sb.Append($"{FieldId}[{FieldType}]");
        }

        /// <summary>
        /// 判断是否引用类型成员
        /// </summary>
        internal bool IsClassType()
        {
            return FieldType == EntityFieldType.String
                                  || FieldType == EntityFieldType.EntityId
                                  || FieldType == EntityFieldType.Binary;
        }
        #endregion

        #region ====Static Eval Methods====
        /// <summary>
        /// Only for test
        /// </summary>
        public static unsafe bool CompareRaw(ushort id, IntPtr vp, int vs, byte[] target)
        {
            KVField found = KVField.Invalid;
            FindFieldFromMvccRaw(id, vp, vs, ulong.MaxValue, ref found);
            var span = new ReadOnlySpan<byte>(found.DataPtr.ToPointer(), found.DataSize);
            return span.SequenceEqual(target);
        }

        public static unsafe string GetString(ushort id, IntPtr vp, int vs, bool mv, ulong ts)
        {
            KVField found = KVField.Invalid;
            if (mv)
                FindFieldFromMvccRaw(id, vp, vs, ts, ref found);
            else
                FindField(id, (byte*)vp.ToPointer(), (uint)vs, ref found);
            return found.IsInvalid() ? null : found.GetString();
        }

        public static unsafe int? GetInt32(ushort id, IntPtr vp, int vs, bool mv, ulong ts)
        {
            KVField found = KVField.Invalid;
            if (mv)
                FindFieldFromMvccRaw(id, vp, vs, ts, ref found);
            else
                FindField(id, (byte*)vp.ToPointer(), (uint)vs, ref found);
            return found.IsInvalid() ? null : found.GetInt32();
        }

        public static unsafe Guid? GetGuid(ushort id, IntPtr vp, int vs, bool mv, ulong ts)
        {
            KVField found = KVField.Invalid;
            if (mv)
                FindFieldFromMvccRaw(id, vp, vs, ts, ref found);
            else
                FindField(id, (byte*)vp.ToPointer(), (uint)vs, ref found);
            return found.IsInvalid() ? null : found.GetGuid();
        }

        public static unsafe byte? GetByte(ushort id, IntPtr vp, int vs, bool mv, ulong ts)
        {
            KVField found = KVField.Invalid;
            if (mv)
                FindFieldFromMvccRaw(id, vp, vs, ts, ref found);
            else
                FindField(id, (byte*)vp.ToPointer(), (uint)vs, ref found);
            return found.IsInvalid() ? null : found.GetByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint GetDataSizeAt(IntPtr ptr, int index)
        {
            var versionPtr = (VersionHead*)(ptr + 2).ToPointer();
            return versionPtr[index].DataSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNearestFullVersionIndex(int index)
        {
            var mode = index % 100; //间隔数与Native一致
            return mode == 0 ? index : index - mode;
        }

        /// <summary>
        /// 获取小于等于指定时间戳的版本索引
        /// </summary>
        /// <returns>-1表示不存在</returns>
        private static unsafe int GetVersionLessOrEqual(IntPtr ptr, ushort count, ulong timestamp, uint* offsetToEnd)
        {
            var versionPtr = (VersionHead*)(ptr + 2).ToPointer();
            //倒序查找
            for (int i = count; i >= 0; i--)
            {
                *offsetToEnd += versionPtr[i].DataSize;
                if (versionPtr[i].Timestamp <= timestamp)
                    return i;
            }

            *offsetToEnd = 0;
            return -1;
        }

        private static unsafe void FindFieldFromMvccRaw(ushort id, IntPtr vp, int vs, ulong ts, ref KVField found)
        {
            ushort* countPtr = (ushort*)vp.ToPointer();
            ushort count = (ushort)(*countPtr >> 1);

            uint offsetToEnd = 0;
            int to = GetVersionLessOrEqual(vp, count, ts, &offsetToEnd);
            int from = GetNearestFullVersionIndex(to);

            byte* dataPtr = (byte*)vp + vs - offsetToEnd;
            uint dataSize = GetDataSizeAt(vp, to);
            KVField temp = new KVField();
            for (int i = to; i >= from; i--) //注意倒序
            {
                FindField(id, dataPtr, dataSize, ref temp);
                if (temp.Id == id)
                {
                    found = temp;
                    break; //因为倒序找到即是最新版本
                }
                if (i > from)
                {
                    dataSize = GetDataSizeAt(vp, i - 1);
                    dataPtr -= dataSize;
                }
            }
        }

        private static unsafe void FindField(ushort id, byte* dataPtr, uint dataSize, ref KVField found)
        {
            byte* cur = dataPtr;
            byte* end = cur + dataSize;
            while (cur < end)
            {
                found.ReadFrom(&cur);
                if (found.Id == id)
                {
                    break;
                }
                cur += found.DataSize;
            }
        }

        private static MethodInfo GetStringMemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetString));
        private static MethodInfo GetInt32MemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetInt32));
        //private static MethodInfo GetInt64MemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetInt64));
        //private static MethodInfo GetFloatMemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetFloat));
        private static MethodInfo GetGuidMemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetGuid));
        private static MethodInfo GetByteMemberInfo => typeof(KVFieldExpression).GetMethod(nameof(GetByte));
        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer writer)
        {
            writer.Write(FieldId);
            writer.Write((byte)FieldType);
        }

        public override void ReadObject(BinSerializer reader)
        {
            FieldId = reader.ReadUInt16();
            FieldType = (EntityFieldType)reader.ReadByte();
        }
        #endregion
    }
}
