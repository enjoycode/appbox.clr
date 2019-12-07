using System;
using System.Text.Json;
using appbox.Caching;
using appbox.Serialization;

namespace appbox.Data
{
    /// <summary>
    /// 封装服务调用的参数列表,减少装拆箱
    /// </summary>
    /// <remarks>目前最多支持5个参数</remarks>
    public struct InvokeArgs
    {
        private AnyValue Arg1;
        private AnyValue Arg2;
        private AnyValue Arg3;
        private AnyValue Arg4;
        private AnyValue Arg5;
        /// <summary>
        /// 0表示空
        /// 255表示第一个参数内的Object为Stream，主要用于Ajax封送
        /// 254表示第一个参数内的Object为BytesChunk，主要用于WebSocket封送
        /// </summary>
        private byte Count;
        private int Postion;

        #region ====From Methods, 仅用于简化服务端编码====
        public static InvokeArgs Empty() { return new InvokeArgs { Count = 0 }; }

        public static InvokeArgs From(IBytesSegment chunk, int offset)
        {
            return new InvokeArgs { Count = 254, Arg1 = AnyValue.From(chunk), Postion = offset };
        }

        public static InvokeArgs From(AnyValue arg)
        {
            return new InvokeArgs() { Arg1 = arg, Count = 1 };
        }

        public static InvokeArgs From(AnyValue arg1, AnyValue arg2)
        {
            return new InvokeArgs() { Arg1 = arg1, Arg2 = arg2, Count = 2 };
        }

        public static InvokeArgs From(AnyValue arg1, AnyValue arg2, AnyValue arg3)
        {
            return new InvokeArgs() { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Count = 3 };
        }

        public static InvokeArgs From(AnyValue arg1, AnyValue arg2, AnyValue arg3, AnyValue arg4)
        {
            return new InvokeArgs() { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Count = 4 };
        }

        public static InvokeArgs From(AnyValue arg1, AnyValue arg2, AnyValue arg3, AnyValue arg4, AnyValue arg5)
        {
            return new InvokeArgs() { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Count = 5 };
        }
        #endregion

        #region ====Set Methods====
        internal void Set(int index, AnyValue value)
        {
            if (index < 0 && index > 4) throw new ArgumentOutOfRangeException(nameof(index));

            switch (index)
            {
                case 0: Arg1 = value; break;
                case 1: Arg2 = value; break;
                case 2: Arg3 = value; break;
                case 3: Arg4 = value; break;
                case 4: Arg5 = value; break;
            }
        }
        #endregion

        #region ====GetXXX Methods====
        private AnyValue Current() //Can't use ref and unsafe pointer
        {
            var curPos = Postion++;
            if (curPos >= Count)
                throw new IndexOutOfRangeException();
            return curPos switch
            {
                0 => Arg1,
                1 => Arg2,
                2 => Arg3,
                3 => Arg4,
                4 => Arg5,
                _ => throw new IndexOutOfRangeException(),
            };
        }

        public bool GetBoolean()
        {
            return Current().BooleanValue;
        }

        public int GetInt32()
        {
            if (Count == byte.MaxValue)
            {
                //从第一个参数的流中读取
                throw new NotImplementedException();
            }
            return Current().Int32Value;
        }

        public string GetString()
        {
            if (Count == 254)
            {
                //var chunk = (IBytesChunk)Arg1.ObjectValue;
                //var jr = new Utf8JsonReader();
                throw new NotImplementedException();
            }
            else if (Count == 255)
            {
                throw new NotImplementedException();
            }
            else
            {
                return (string)Current().ObjectValue;
            }
        }

        public object GetObject()
        {
            return Current().ObjectValue;
        }

        public ObjectArray GetObjectArray()
        {
            return (ObjectArray)Current().ObjectValue;
        }

        public T Get<T>()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ====Serialization====
        internal void WriteObject(BinSerializer bs)
        {
            bs.Write(Count);
            if (Count == byte.MaxValue)
            {
                throw new NotImplementedException();
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    switch (i)
                    {
                        case 0: Arg1.WriteObject(bs); break;
                        case 1: Arg2.WriteObject(bs); break;
                        case 2: Arg3.WriteObject(bs); break;
                        case 3: Arg4.WriteObject(bs); break;
                        case 4: Arg5.WriteObject(bs); break;
                    }
                }
            }
        }

        internal void ReadObject(BinSerializer bs)
        {
            Count = bs.ReadByte();
            if (Count == byte.MaxValue)
            {
                throw new NotImplementedException();
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    switch (i)
                    {
                        case 0: Arg1.ReadObject(bs); break;
                        case 1: Arg2.ReadObject(bs); break;
                        case 2: Arg3.ReadObject(bs); break;
                        case 3: Arg4.ReadObject(bs); break;
                        case 4: Arg5.ReadObject(bs); break;
                    }
                }
            }
        }
        #endregion

    }

}
