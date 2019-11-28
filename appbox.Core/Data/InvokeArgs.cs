using System;
using appbox.Caching;
using appbox.Serialization;

namespace appbox.Data
{

    public struct InvokeArgs : IBinSerializable
    {

        #region ====Statics====
        private static ObjectPool<ValueArg> valueArgPool;
        private static ObjectPool<ObjectArg> objectArgPool;

        public static readonly InvokeArgs Empty = new InvokeArgs();

        //private static int newCount;

        static InvokeArgs()
        {
            int poolSize = 32; //todo:确认合理值
            valueArgPool = new ObjectPool<ValueArg>(p => new ValueArg(), null, poolSize);
            objectArgPool = new ObjectPool<ObjectArg>(p => new ObjectArg(), (obj) => obj.ObjectRef = null, poolSize);
        }
        #endregion

        private IInvokeArg first;
        private IInvokeArg current;

        #region ====Add Methods====
        public InvokeArgs Add(int value)
        {
            var arg = valueArgPool.Pop();
            arg.SetValue(value);
            AddArg(arg);
            return this;
        }

        public InvokeArgs Add(long value)
        {
            var arg = valueArgPool.Pop();
            arg.SetValue(value);
            AddArg(arg);
            return this;
        }

        public InvokeArgs Add(bool value)
        {
            var arg = valueArgPool.Pop();
            arg.SetValue(value);
            AddArg(arg);
            return this;
        }

        public InvokeArgs Add(Guid value)
        {
            var arg = valueArgPool.Pop();
            arg.SetValue(value);
            AddArg(arg);
            return this;
        }

        public InvokeArgs Add(object value)
        {
            var arg = objectArgPool.Pop();
            arg.SetValue(value);
            AddArg(arg);
            return this;
        }

        private void AddArg(IInvokeArg arg)
        {
            arg.Next = null;

            if (first == null)
            {
                first = current = arg;
            }
            else
            {
                current.Next = arg;
                current = arg;
            }
        }
        #endregion

        #region ====Get Methods====
        //注意：GetXXX后自动释放缓存

        /// <summary>
        /// 仅服务端内部使用
        /// </summary>
        public void BeginGet()
        {
            current = first;
        }

        public object GetObject()
        {
            var temp = current;
            var res = current.GetObject();
            current = current.Next;
            temp.PushToPool();
            return res;
        }

        public string GetString()
        {
            return (string)GetObject();
        }

        public ObjectArray GetObjectArray()
        {
            return (ObjectArray)GetObject();
        }

        public Guid GetGuid()
        {
            var temp = current;
            var res = current.GetGuid();
            current = current.Next;
            temp.PushToPool();
            return res;
        }

        public DateTime GetDateTime()
        {
            var temp = current;
            var res = current.GetDateTime();
            current = current.Next;
            temp.PushToPool();
            return res;
        }

        public int GetInt32()
        {
            var temp = current;
            var res = current.GetInt32();
            current = current.Next;
            temp.PushToPool();
            return res;
        }

        public bool GetBoolean()
        {
            var temp = current;
            var res = current.GetBoolean();
            current = current.Next;
            temp.PushToPool();
            return res;
        }
        #endregion

        #region ====Pool Method====
        public void ReturnUngetArgs()
        {
            //注意：直接从current开始，其他的在GetXXX()时已释放，所以不能设置 current = first;
            IInvokeArg temp = null;
            while (current != null)
            {
                temp = current.Next;
                current.PushToPool();
                Log.Debug($"归还 current.value = {current.GetObject()}");
                current = temp;
            }

            first = current = null;
        }
        #endregion

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            current = first;
            IInvokeArg temp = null;
            uint index = 0;
            while (current != null)
            {
                temp = current.Next;
                bs.Write(++index);
                current.WriteObject(bs);
                current.PushToPool(); //注意：序列化后自动释放缓存
                current = temp;
            }

            bs.Write((uint)0);
        }

        public void ReadObject(BinSerializer bs)
        {
            uint index = bs.ReadUInt32();
            while (index != 0)
            {
                ArgTypeCode typeCode = (ArgTypeCode)bs.ReadByte();
                IInvokeArg arg = null;
                if (typeCode == ArgTypeCode.Object)
                    arg = objectArgPool.Pop();
                else
                    arg = valueArgPool.Pop();
                arg.ReadObject(bs, typeCode);
                AddArg(arg);

                index = bs.ReadUInt32();
            }

            //this.current = this.first;
        }
        #endregion

        #region ====ValueArg & ObjectArg====
        private class ValueArg : IInvokeArg
        {
            private ArgTypeCode typeCode;
            private Guid Data;

            public IInvokeArg Next { get; set; }

            //public ValueArg()
            //{
            //    Console.WriteLine(System.Threading.Interlocked.Increment(ref newCount));
            //}

            public unsafe void SetValue(int value)
            {
                typeCode = ArgTypeCode.Int32;
                Guid temp = Guid.Empty;
                int* ptr = (int*)&temp;
                *ptr = value;
                Data = temp;
            }

            public unsafe void SetValue(long value)
            {
                typeCode = ArgTypeCode.Int64;
                Guid temp = Guid.Empty;
                long* ptr = (long*)&temp;
                *ptr = value;
                Data = temp;
            }

            public unsafe void SetValue(bool value)
            {
                typeCode = ArgTypeCode.Boolean;
                Guid temp = Guid.Empty;
                int* ptr = (int*)&temp;
                *ptr = value == true ? 1 : 0;
                Data = temp;
            }

            public void SetValue(Guid value)
            {
                typeCode = ArgTypeCode.Guid;
                Data = value;
            }

            public object GetObject()
            {
                switch (typeCode)
                {
                    case ArgTypeCode.Boolean:
                        return GetBoolean();
                    case ArgTypeCode.Int32:
                        return GetInt32();
                    case ArgTypeCode.Guid:
                        return GetGuid();
                    default:
                        throw ExceptionHelper.NotImplemented();
                }
            }

            public Guid GetGuid()
            {
                if (typeCode != ArgTypeCode.Guid)
                    throw new Exception("InvokeArg is not Guid");

                return Data;
            }

            public unsafe DateTime GetDateTime()
            {
                if (typeCode != ArgTypeCode.DateTime)
                    throw new Exception("InvokeArg is not DateTime");

                Guid temp = Data;
                DateTime* ptr = (DateTime*)&temp;
                return *ptr;
            }

            public unsafe int GetInt32()
            {
                if (typeCode == ArgTypeCode.Int64)
                    return (int)GetInt64();
                if (typeCode != ArgTypeCode.Int32)
                    throw new Exception("InvokeArg is not Int32");

                Guid temp = Data;
                int* ptr = (int*)&temp;
                return *ptr;
            }

            public unsafe long GetInt64()
            {
                if (typeCode == ArgTypeCode.Int32)
                    return GetInt32();
                if (typeCode != ArgTypeCode.Int64)
                    throw new Exception("InvokeArg is not Int64");

                Guid temp = Data;
                long* ptr = (long*)&temp;
                return *ptr;
            }

            public unsafe bool GetBoolean()
            {
                if (typeCode != ArgTypeCode.Boolean)
                    throw new Exception("InvokeArg is not Boolean");

                Guid temp = Data;
                int* ptr = (int*)&temp;
                if (*ptr == 0)
                    return false;
                return true;
            }

            public void PushToPool()
            {
                valueArgPool.Push(this);
            }

            public void WriteObject(BinSerializer bs)
            {
                bs.Write((byte)typeCode);
                bs.Write(Data);
            }

            public void ReadObject(BinSerializer bs, ArgTypeCode typeCode)
            {
                this.typeCode = typeCode;
                Data = bs.ReadGuid();
            }
        }

        private enum ArgTypeCode : byte
        {
            Object,
            Boolean,
            Char,
            SByte,
            Byte,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Single,
            Double,
            Decimal,
            DateTime,
            Guid,
        }

        private class ObjectArg : IInvokeArg
        {
            public IInvokeArg Next { get; set; }
            public object ObjectRef;

            //public ObjectArg()
            //{
            //    Console.WriteLine(System.Threading.Interlocked.Increment(ref newCount));
            //}

            public void SetValue(object value)
            {
                ObjectRef = value;
            }

            public object GetObject()
            {
                return ObjectRef;
            }

            public Guid GetGuid()
            {
                if (ObjectRef is string value) //专门用于处理Web前端传回的参数
                {
                    if (string.IsNullOrEmpty(value))
                        return Guid.Empty;
                    return new Guid(value); //转换不成功则报错
                }

                return (Guid)ObjectRef;
            }

            public DateTime GetDateTime()
            {
                if (ObjectRef is string value) //同上
                {
                    if (string.IsNullOrEmpty(value))
                        return DateTime.MinValue;
                    return DateTime.Parse(value);
                }

                return (DateTime)ObjectRef;
            }

            public bool GetBoolean()
            {
                return Convert.ToBoolean(ObjectRef);
            }

            public int GetInt32()
            {
                return Convert.ToInt32(ObjectRef);
            }

            public void PushToPool()
            {
                ObjectRef = null;
                objectArgPool.Push(this);
            }

            public void WriteObject(BinSerializer bs)
            {
                bs.Write((byte)ArgTypeCode.Object);
                bs.Serialize(ObjectRef);
            }

            public void ReadObject(BinSerializer bs, ArgTypeCode typeCode)
            {
                ObjectRef = bs.Deserialize();
            }
        }

        private interface IInvokeArg
        {
            IInvokeArg Next { get; set; }

            object GetObject();

            Guid GetGuid();

            bool GetBoolean();

            int GetInt32();

            DateTime GetDateTime();

            void PushToPool();

            void WriteObject(BinSerializer bs);

            void ReadObject(BinSerializer bs, ArgTypeCode typeCode);
        }
        #endregion

        #region ====Debug Methods====
        internal void DumpArgs()
        {
            var cur = first;
            var sb = StringBuilderCache.Acquire();
            sb.AppendLine("====Args Begin====");
            IInvokeArg temp;
            int index = 0;
            while (cur != null)
            {
                temp = cur.Next;
                var value = cur.GetObject();
                if (value == null)
                    sb.AppendLine($"[{index}] Type=Null Value=Null");
                else
                    sb.AppendLine($"[{index}] Type={value.GetType()} Value={value}");
                index++;
                cur = temp;
            }
            sb.AppendLine("====Args End======");
            Console.Write(StringBuilderCache.GetStringAndRelease(sb));
        }
        #endregion
    }

}
