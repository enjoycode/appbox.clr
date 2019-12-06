using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace appbox.Core.Tests
{
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct AnyValue
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
        [FieldOffset(16)]
        public object ObjectValue;

        public static implicit operator AnyValue(int v)
        {
            return new AnyValue { Int32Value = v };
        }

        //public static implicit operator AnyValue(Serialization.IBinSerializable obj)
        //{
        //    return new AnyValue { ObjectValue = obj };
        //}

        public static implicit operator AnyValue(Data.Entity obj)
        {
            return new AnyValue { ObjectValue = obj };
        }
    }

    public /*ref*/ struct InvokeArgs
    {
        private AnyValue Arg1;
        private AnyValue Arg2;
        private AnyValue Arg3;
        private byte Count;
        private int Postion;

        public static InvokeArgs Make(AnyValue arg)
        {
            return new InvokeArgs() { Arg1 = arg, Count = 1 };
        }

        private AnyValue Current() //Can't use ref and unsafe pointer
        {
            var curPos = Postion++;
            return curPos switch
            {
                0 => Arg1,
                1 => Arg2,
                2 => Arg3,
                _ => throw new IndexOutOfRangeException(),
            };
        }

        public int GetInt32()
        {
            return Current().Int32Value;
        }

        public T Get<T>()
        {
            throw new NotImplementedException();
        }
    }

    public interface IService
    {
        ValueTask<AnyValue> Invoke(ReadOnlyMemory<char> method, /*ref*/ InvokeArgs args);
    }

    public class HelloService : IService
    {
        public async ValueTask<int> SayHello(int delay)
        {
            await Task.Delay(delay);
            return 1234;
        }

        public async ValueTask<AnyValue> Invoke(ReadOnlyMemory<char> method, /*ref*/ InvokeArgs args)
        {
            return await SayHello(args.GetInt32());
        }
    }

    public struct InvokeRequire
    {
        public string Service;
        public InvokeArgs Args;
    }

    public class InvokeArgsTest
    {

        [Fact]
        public async Task Run()
        {
            IService service = new HelloService();

            var args = InvokeArgs.Make(123);
            //var req = new InvokeRequire();
            await service.Invoke("SayHello".AsMemory(), /*ref*/ args);
        }
    }

}
