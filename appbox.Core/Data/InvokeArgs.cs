using System;
using System.Buffers;
using System.Diagnostics;
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
        /// 254表示第一个参数内的Object为当前BytesSegment，第二个参数为last BytesSegment, 主要用于WebSocket封送
        /// </summary>
        private byte Count;
        private int Postion;
        private JsonReaderState? JsonState;

        private const byte FromWebSocket = 254;
        private const byte FromWebStream = 255; //仅存在于Host进程

        #region ====From Methods, 仅用于简化服务端编码====
        public static InvokeArgs Empty() { return new InvokeArgs { Count = 0 }; }

        public static InvokeArgs From(BytesSegment last, int offset)
        {
            if (offset == -1) return Empty();

            if (last.Next != null)
                throw new ArgumentException(nameof(last));
            return new InvokeArgs
            {
                Count = FromWebSocket,
                Arg1 = AnyValue.From(last.First), //当前包，新建是指向第一包
                Arg2 = AnyValue.From(last), //最后一包
                Postion = offset
            };
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

        #region ====Methods====
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

        /// <summary>
        /// 归还缓存块
        /// </summary>
        /// <remarks>
        /// 1.客户端调用系统服务后;
        /// 2.客户端转发调用至子进程后;
        /// 3.子进程调用转发的请求后.
        /// </remarks>
        internal void ReturnBuffer()
        {
            if (Count == FromWebSocket)
            {
                Debug.Assert(Arg1.ObjectValue != null);
                BytesSegment.ReturnAll((BytesSegment)Arg1.ObjectValue);
            }
        }

        internal string DebugString()
        {
            if (Count == FromWebSocket)
                return $"FromWebSocket";
            return $"{Count}";
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

        private Utf8JsonReader ReadJsonArg()
        {
            var cur = (BytesSegment)Arg1.ObjectValue;
            var last = (BytesSegment)Arg2.ObjectValue;
            Utf8JsonReader jr;
            if (last == null || cur == last) //一开始就一包或读到只剩一包
                jr = new Utf8JsonReader(cur.Buffer.AsSpan(Postion, cur.Length - Postion),
                                        true, JsonState ?? (default));
            else
                jr = new Utf8JsonReader(new ReadOnlySequence<byte>(cur, Postion, last, last.Length),
                                        cur.Next == null, JsonState ?? (default));

            if (!jr.Read() || jr.TokenType == JsonTokenType.EndArray)
                throw new Exception("Can't read from utf8 bytes");

            //首次读到参数数组开始，再读一次
            if (jr.TokenType == JsonTokenType.StartArray && !JsonState.HasValue) 
            {
                if (!jr.Read()) throw new Exception("Can't read from utf8 bytes");
            }

            //非Object及数组开始直接移动位置及状态
            if (jr.TokenType != JsonTokenType.StartArray && jr.TokenType != JsonTokenType.StartObject)
            {
                AdvancePosition(ref jr);
            }
            
            return jr;
        }

        private void AdvancePosition(ref Utf8JsonReader reader)
        {
            var cur = (BytesSegment)Arg1.ObjectValue;
            var last = (BytesSegment)Arg2.ObjectValue;
            if (last == null || cur == last)
            {
                Postion += (int)reader.BytesConsumed;
            }
            else
            {
                Arg1.ObjectValue = reader.Position.GetObject(); //是否考虑直接归还之前已读完的
                Postion = reader.Position.GetInteger();
            }
            JsonState = reader.CurrentState;
        }

        //TODO: *****other types

        public bool GetBoolean()
        {
            if (Count == FromWebSocket)
                return ReadJsonArg().GetBoolean();
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().BooleanValue;
        }

        public byte GetByte()
        {
            if (Count == FromWebSocket)
                return ReadJsonArg().GetByte();
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().ByteValue;
        }

        public int GetInt32()
        {
            if (Count == FromWebSocket)
                return ReadJsonArg().GetInt32();
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().Int32Value;
        }

        public string GetString()
        {
            if (Count == FromWebSocket)
            {
                return ReadJsonArg().GetString();
            }
            if (Count == FromWebStream)
            {
                throw new NotImplementedException();
            }

            return (string)Current().ObjectValue;
        }

        public DateTime GetDateTime()
        {
            if (Count == FromWebSocket)
                return ReadJsonArg().GetDateTime();
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().DateTimeValue;
        }

        public Guid GetGuid()
        {
            if (Count == FromWebSocket)
                return ReadJsonArg().GetGuid();
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().GuidValue;
        }

        public object GetObject()
        {
            if (Count == FromWebSocket)
            {
                var jr = ReadJsonArg();
                var res = jr.ReadObject(new ReadedObjects()); //Don't use Deserialize,因已读取StartObject
                AdvancePosition(ref jr); //注意移到位置及状态
                return res;
            }
            if (Count == FromWebStream)
                throw new NotImplementedException();
            return Current().ObjectValue;
        }

        public ObjectArray GetObjectArray()
        {
            if (Count == FromWebSocket || Count == FromWebStream)
                throw new NotImplementedException();
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
            if (Count == FromWebSocket)
            {
                //不用写入Position,第一包已跳过
                var temp = (BytesSegment)Arg1.ObjectValue;
                while (temp != null)
                {
                    if (temp == temp.First) //第一包跳过头部数据
                    {
                        bs.Write(temp.Length - Postion);
                        bs.Stream.Write(temp.Memory.Slice(Postion).Span);
                    }
                    else
                    {
                        bs.Write(temp.Length);
                        bs.Stream.Write(temp.Memory.Span);
                    }
                    temp = (BytesSegment)temp.Next;
                }
                bs.Write(0);
            }
            else if (Count == FromWebStream)
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
            if (Count == FromWebSocket)
            {
                int len;
                BytesSegment cur = null;
                while (true)
                {
                    len = bs.ReadInt32();
                    if (len == 0) break;

                    var temp = BytesSegment.Rent();
                    bs.Stream.Read(temp.Buffer.AsSpan(0, len));
                    temp.Length = len;
                    if (cur != null)
                        cur.Append(temp);
                    cur = temp;
                }

                Arg1 = AnyValue.From(cur.First);
                Arg2 = AnyValue.From(cur);
            }
            else if (Count == FromWebStream)
            {
                throw new NotImplementedException();
            }
            else
            {
                var temp = new AnyValue();
                for (int i = 0; i < Count; i++)
                {
                    temp.ReadObject(bs);
                    switch (i)
                    {
                        case 0: Arg1 = temp; break;
                        case 1: Arg2 = temp; break;
                        case 2: Arg3 = temp; break;
                        case 3: Arg4 = temp; break;
                        case 4: Arg5 = temp; break;
                    }
                }
            }
        }
        #endregion

    }

}
