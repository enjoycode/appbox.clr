using System;
using Xunit;
using System.Linq.Expressions;
using appbox.Data;
using appbox.Models;
using Xunit.Abstractions;
using System.IO;
using appbox.Serialization;

namespace appbox.Core.Tests
{
    public class ExpressionTest
    {
        private readonly ITestOutputHelper output;

        public ExpressionTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AndAlsoTest()
        {
            // http://www.cnblogs.com/zzagly/p/5849491.html
            // https://stackoverflow.com/questions/13967523/andalso-between-several-expressionfunct-bool-referenced-from-scope

            Expression<Func<IntPtr, bool>> exp1 = t1 => t1 != IntPtr.Zero;
            Expression<Func<IntPtr, bool>> exp2 = t2 => t2 == new IntPtr(123);

            var para = exp1.Parameters[0];
            var exp3 = Expression.Lambda<Func<IntPtr, bool>>(
                Expression.AndAlso(exp1.Body, Expression.Invoke(exp2, para))
                , para);
            Console.WriteLine(exp3);

            var d = exp3.Compile();
            Assert.True(d(new IntPtr(123)));
            Assert.False(d(IntPtr.Zero));
            Assert.False(d(new IntPtr(1)));
        }

        /// <summary>
        /// 测试字符串用utf8字节比较与转换为C# string的比较的性能
        /// </summary>
        [Fact]
        public unsafe void StringComparePerfTest()
        {
            var data = System.Text.Encoding.UTF8.GetBytes("Admin");

            Expression<Func<IntPtr, bool>> exp1 = t => GetStringFromPtr(t, data.Length) == "Admin";
            //Expression<Func<IntPtr, bool>> exp2 = t => GetSpanFromPtr(t, dataLen).SequenceEqual(GetSpanFromPtr(t, dataLen));
            Expression<Func<IntPtr, bool>> exp2 = t => SpanCompare(t, data.Length);

            var d1 = exp1.Compile();
            var d2 = exp2.Compile();

            var loopCount = 5000000;
            fixed (byte* ptr = data)
            {
                IntPtr dataPtr = new IntPtr(ptr);
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                for (int i = 0; i < loopCount; i++)
                {
                    d1(dataPtr);
                }
                sw.Stop();
                output.WriteLine($"C# String比较耗时: {sw.ElapsedMilliseconds}"); //Debug:240 Release: 180

                sw.Restart();
                for (int i = 0; i < loopCount; i++)
                {
                    d2(dataPtr);
                }
                sw.Stop();
                output.WriteLine($"利用Span比较耗时:   {sw.ElapsedMilliseconds}"); //Debug:100 Release: 20
            }
        }

        private static unsafe string GetStringFromPtr(IntPtr ptr, int dataSize)
        {
            return new string((sbyte*)ptr, 0, dataSize, System.Text.Encoding.UTF8);
        }

        //表达式树不能包含ReadOnlySpan
        //private static unsafe ReadOnlySpan<byte> GetSpanFromPtr(IntPtr ptr, int dataSize)
        //{
        //    return new ReadOnlySpan<byte>(ptr.ToPointer(), dataSize);
        //}

        private static unsafe bool SpanCompare(IntPtr ptr, int dataSize)
        {
            return new ReadOnlySpan<byte>(ptr.ToPointer(), dataSize)
                .SequenceEqual(new ReadOnlySpan<byte>(ptr.ToPointer(), dataSize));
        }

        private byte[] SerializeExpression(Expressions.Expression exp)
        {
            byte[] data = null;
            using (var ms = new MemoryStream(1024))
            {
                var cf = new BinSerializer(ms);
                try { cf.Serialize(exp); }
                catch (Exception) { throw; }
                finally { cf.Clear(); }

                ms.Close();
                data = ms.ToArray();
            }
            return data;
        }

        [Fact]
        public void PrimitiveExpressionSerializeTest()
        {
            var exp = new Expressions.PrimitiveExpression("无");
            var data = SerializeExpression(exp);
            output.WriteLine($"长度: {data.Length}"); //6
            output.WriteLine($"内容: {StringHelper.ToHexString(data)}"); //401002E697A0
        }

#if FUTURE
        [Fact]
        public unsafe void ExpressionPerfTest()
        {
            var data = StringHelper.FromHexString("00000020E4D8BEBE75592200000080000600004141414141310501880100000000000000000002060000414141414131");
            var target = System.Text.Encoding.UTF8.GetBytes("AAAAA1");
            var loopCount = 5000000;
            fixed (byte* ptr = data)
            {
                IntPtr vp = new IntPtr(ptr);
                Expression<Func<IntPtr, bool>> exp1 = t => Expressions.KVFieldExpression.GetString(
                    Consts.EMPLOEE_NAME_ID, t, data.Length, true, ulong.MaxValue) == "AAAAA1";
                Expression<Func<IntPtr, bool>> exp2 = t => Expressions.KVFieldExpression.CompareRaw(
                    Consts.EMPLOEE_NAME_ID, t, data.Length, target);

                var d1 = exp1.Compile();
                var d2 = exp2.Compile();

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                for (int i = 0; i < loopCount; i++)
                {
                    d1(vp);
                }
                sw.Stop();
                output.WriteLine($"1耗时: {sw.ElapsedMilliseconds}"); //Release: 310

                sw.Restart();
                for (int i = 0; i < loopCount; i++)
                {
                    d2(vp);
                }
                sw.Stop();
                output.WriteLine($"2耗时: {sw.ElapsedMilliseconds}"); //Debug: Release: 135
            }
        }

        [Fact]
        public unsafe void KVFieldExpressionTest_NoneMvcc()
        {
            var data = StringHelper.FromHexString("400005000041646D696E800005000041646D696E");
            var field = new Expressions.KVFieldExpression(Consts.EMPLOEE_NAME_ID, EntityFieldType.String);
            Expressions.Expression filter = field == "Admin";

            var ctx = TestHelper.GetMockExpressionContext();
            var body = filter.ToLinqExpression(ctx);
            var exp = Expression.Lambda<KVFilterFunc>(body,
                ctx.GetParameter("vp"), ctx.GetParameter("vs"), ctx.GetParameter("mv"), ctx.GetParameter("ts"));
            var func = exp.Compile();

            fixed (byte* ptr = data)
            {
                IntPtr dataPtr = new IntPtr(ptr);
                Assert.True(func(dataPtr, data.Length, false, 0));
            }
        }

        [Fact]
        public unsafe void KVFieldExpressionTest_Mvcc()
        {
            var data = StringHelper.FromHexString("00000020E4D8BEBE75592200000080000600004141414141310501880100000000000000000002060000414141414131");
            var field = new Expressions.KVFieldExpression(Consts.EMPLOEE_NAME_ID, EntityFieldType.String);
            Expressions.Expression filter = field == "AAAAA1";

            var ctx = TestHelper.GetMockExpressionContext();
            var body = filter.ToLinqExpression(ctx);
            var exp = Expression.Lambda<KVFilterFunc>(body,
                ctx.GetParameter("vp"), ctx.GetParameter("vs"), ctx.GetParameter("mv"), ctx.GetParameter("ts"));
            var func = exp.Compile();

            fixed (byte* ptr = data)
            {
                IntPtr dataPtr = new IntPtr(ptr);
                Assert.True(func(dataPtr, data.Length, true, ulong.MaxValue));
            }
        }

        [Fact]
        public void NullableTest()
        {
            var f1 = new Expressions.KVFieldExpression(1, EntityFieldType.Int32);
            var f2 = new Expressions.KVFieldExpression(2, EntityFieldType.Guid);

            Expressions.Expression exp = f1 > 5 & f2 == null;
            var expString1 = exp.ToString();

            var linqExp = exp.ToLinqExpression(TestHelper.GetMockExpressionContext());
            var expString2 = linqExp.ToString();
        }

        /// <summary>
        /// 序列化测试，用于存储层反序列化测试
        /// </summary>
        [Fact]
        public void KVFieldExpressionSerializeTest()
        {
            var field = new Expressions.KVFieldExpression(Consts.EMPLOEE_NAME_ID, EntityFieldType.String);
            //var value = new Expressions.PrimitiveExpression("AAAAA1");
            var exp = field == "AAAAA1";
            var data = SerializeExpression(exp);
            output.WriteLine($"长度: {data.Length}"); //15
            output.WriteLine($"内容: {StringHelper.ToHexString(data)}"); //3F4E8000010040100C414141414131
        }
#endif
    }
}
