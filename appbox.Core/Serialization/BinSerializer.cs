using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using appbox.Models;
using appbox.Data;
using appbox.Expressions;

namespace appbox.Serialization
{
    /// <summary>
    /// 二进制序列化器
    /// </summary>
    /// <remarks>
    /// 注意：所有Write()方法不写入类型信息，只写入数据部分
    /// </remarks>
    public sealed class BinSerializer
    {
        #region ====Statics====
        [ThreadStatic]
        private static BinSerializer threadInstance;

        public static BinSerializer ThreadInstance
        {
            get
            {
                if (threadInstance == null)
                    threadInstance = new BinSerializer();
                return threadInstance;
            }
        }

        private static readonly Dictionary<Type, TypeSerializer> knownTypes = new Dictionary<Type, TypeSerializer>(256);
        private static readonly Dictionary<PayloadType, TypeSerializer> sysKnownTypesIndexer = new Dictionary<PayloadType, TypeSerializer>(256);
        private static readonly Dictionary<ExtKnownTypeID, TypeSerializer> extKnownTypesIndexer = new Dictionary<ExtKnownTypeID, TypeSerializer>(256);

        private static readonly ByteSerializer ByteSerializer = new ByteSerializer();
        private static readonly BooleanSerializer BooleanSerializer = new BooleanSerializer();
        private static readonly Int32Serializer Int32Serializer = new Int32Serializer();
        private static readonly Int64Serializer Int64Serializer = new Int64Serializer();
        private static readonly GuidSerializer GuidSerializer = new GuidSerializer();
        private static readonly DateTimeSerializer DateTimeSerializer = new DateTimeSerializer();
        private static readonly StringSerializer StringSerializer = new StringSerializer();
        private static readonly FloatSerializer FloatSerializer = new FloatSerializer();
        private static readonly DoubleSerializer DoubleSerializer = new DoubleSerializer();

        private static readonly ArraySerializer ArraySerializer = new ArraySerializer();
        private static readonly ListSerializer ListSerializer = new ListSerializer();
        private static readonly DictionarySerializer DictionarySerializer = new DictionarySerializer();

        static BinSerializer()
        {
            //基元值类型
            RegisterKnownType(BooleanSerializer);
            RegisterKnownType(ByteSerializer);
            RegisterKnownType(Int32Serializer);
            RegisterKnownType(Int64Serializer);
            RegisterKnownType(GuidSerializer);
            RegisterKnownType(DateTimeSerializer);
            RegisterKnownType(StringSerializer);
            RegisterKnownType(FloatSerializer);
            RegisterKnownType(DoubleSerializer);
            //Collections
            RegisterKnownType(ArraySerializer);
            RegisterKnownType(ListSerializer);
            RegisterKnownType(DictionarySerializer);

            //模型及通用类型
            //RegisterKnownType(new EnumSerializer(typeof(ModelType), 1, 1));

            RegisterKnownType(new UserSerializer(PayloadType.ModelBase, typeof(ModelBase), null)); //特殊类型
            RegisterKnownType(new UserSerializer(PayloadType.DataStoreModel, typeof(DataStoreModel), () => new DataStoreModel()));
            //RegisterKnownType(new UserSerializer(typeof(CheckoutInfo), 1, 10, () => new CheckoutInfo()));
            //RegisterKnownType(new UserSerializer(typeof(CheckoutResult), 1, 11, () => new CheckoutResult()));
            //RegisterKnownType(new UserSerializer(typeof(PublishModels), 1, 12, () => new PublishModels()));
            //RegisterKnownType(new UserSerializer(PayloadType.Resource, typeof(Resource), () => new Resource()));
            RegisterKnownType(new UserSerializer(PayloadType.TreeNodePath, typeof(TreeNodePath), () => new TreeNodePath()));
            //RegisterKnownType(new UserSerializer(PayloadType.ActivityViewInfo, typeof(ActivityViewInfo), () => new ActivityViewInfo()));
            //RegisterKnownType(new UserSerializer(PayloadType.HumanActionResult, typeof(HumanActionResult), () => new HumanActionResult()));
            //RegisterKnownType(new UserSerializer(PayloadType.PermissionNode, typeof(PermissionNode), () => new PermissionNode()));
            //RegisterKnownType(new UserSerializer(PayloadType.ResourceModel, typeof(ResourceModel), () => new ResourceModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.ResourceValue, typeof(ResourceValue), () => new ResourceValue()));
            RegisterKnownType(new UserSerializer(PayloadType.ApplicationModel, typeof(ApplicationModel), () => new ApplicationModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.ApplicationAssembly, typeof(ApplicationAssembly), () => new ApplicationAssembly()));
            //RegisterKnownType(new UserSerializer(PayloadType.EnumModel, typeof(EnumModel), () => new EnumModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.EnumModelItem, typeof(EnumModelItem), () => new EnumModelItem()));
            RegisterKnownType(new UserSerializer(PayloadType.ViewModel, typeof(ViewModel), () => new ViewModel()));

            //RegisterKnownType(new UserSerializer(PayloadType.ReportModel, typeof(ReportModel), () => new ReportModel()));
            RegisterKnownType(new UserSerializer(PayloadType.ServiceModel, typeof(ServiceModel), () => new ServiceModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.WorkflowModel, typeof(WorkflowModel), () => new WorkflowModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.StartActivityModel, typeof(StartActivityModel), () => new StartActivityModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.SingleHumanActivityModel, typeof(SingleHumanActivityModel), () => new SingleHumanActivityModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.DecisionActivityModel, typeof(DecisionActivityModel), () => new DecisionActivityModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.AutomationActivityModel, typeof(AutomationActivityModel), () => new AutomationActivityModel()));
            RegisterKnownType(new UserSerializer(PayloadType.EntityModel, typeof(EntityModel), () => new EntityModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.InheritEntityModel, typeof(InheritEntityModel), () => new InheritEntityModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.AutoNumberModel, typeof(AutoNumberModel), () => new AutoNumberModel()));
            RegisterKnownType(new UserSerializer(PayloadType.DataFieldModel, typeof(DataFieldModel), () => new DataFieldModel()));
            RegisterKnownType(new UserSerializer(PayloadType.EntityRefModel, typeof(EntityRefModel), () => new EntityRefModel()));
            RegisterKnownType(new UserSerializer(PayloadType.EntitySetModel, typeof(EntitySetModel), () => new EntitySetModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.FieldSetModel, typeof(FieldSetModel), () => new FieldSetModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.AggregationRefModel, typeof(AggregationRefFieldModel), () => new AggregationRefFieldModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.ImageRefModel, typeof(ImageRefModel), () => new ImageRefModel()));
            RegisterKnownType(new UserSerializer(PayloadType.PermissionModel, typeof(PermissionModel), () => new PermissionModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.EventModel, typeof(EventModel), () => new EventModel()));
            RegisterKnownType(new UserSerializer(PayloadType.ModelFolder, typeof(ModelFolder), () => new ModelFolder()));
            //RegisterKnownType(new UserSerializer(PayloadType.DataTable, typeof(DataTable), () => new DataTable()));
            //RegisterKnownType(new UserSerializer(PayloadType.ObjectArray, typeof(ObjectArray), () => new ObjectArray()));
            RegisterKnownType(new UserSerializer(PayloadType.SysStoreOptions, typeof(SysStoreOptions), () => new SysStoreOptions()));
            RegisterKnownType(new UserSerializer(PayloadType.SqlStoreOptions, typeof(SqlStoreOptions), () => new SqlStoreOptions()));
            RegisterKnownType(new UserSerializer(PayloadType.EntityIndexModel, typeof(EntityIndexModel), () => new EntityIndexModel()));
            RegisterKnownType(new UserSerializer(PayloadType.SqlIndexModel, typeof(SqlIndexModel), () => new SqlIndexModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.AppPackage, typeof(AppPackage), () => new AppPackage()));

            //模型实例
            RegisterKnownType(new UserSerializer(PayloadType.Entity, typeof(Entity), () => new Entity()));
            RegisterKnownType(new UserSerializer(PayloadType.EntityList, typeof(EntityList), () => new EntityList()));

            //表达式
            //RegisterKnownType(new UserSerializer(PayloadType.FieldExpression, typeof(FieldExpression), () => new FieldExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.FormCreationExpression, typeof(FormCreationExpression), () => new FormCreationExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.LambdaExpression, typeof(LambdaExpression), () => new LambdaExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.AggregationRefFieldExpression, typeof(AggregationRefFieldExpression), () => new AggregationRefFieldExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.EntityExpression, typeof(EntityExpression), () => new EntityExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.EnumItemExpression, typeof(EnumItemExpression), () => new EnumItemExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.InvokeSysFuncExpression, typeof(InvokeSysFuncExpression), () => new InvokeSysFuncExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.InvokeGuiFuncExpression, typeof(InvokeGuiFuncExpression), () => new InvokeGuiFuncExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.InvokeDynamicExpression, typeof(InvokeDynamicExpression), () => new InvokeDynamicExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.InvokeServiceAsyncExpression, typeof(InvokeServiceAsyncExpression), () => new InvokeServiceAsyncExpression()));
            RegisterKnownType(new UserSerializer(PayloadType.KVFieldExpression, typeof(KVFieldExpression), () => new KVFieldExpression()));
            RegisterKnownType(new UserSerializer(PayloadType.PrimitiveExpression, typeof(PrimitiveExpression), () => new PrimitiveExpression()));
            RegisterKnownType(new UserSerializer(PayloadType.BinaryExpression, typeof(BinaryExpression), () => new BinaryExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.IdentifierExpression, typeof(IdentifierExpression), () => new IdentifierExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.MemberAccessExpression, typeof(MemberAccessExpression), () => new MemberAccessExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.EventAction, typeof(EventAction), () => new EventAction()));
            //RegisterKnownType(new UserSerializer(PayloadType.BlockExpression, typeof(BlockExpression), () => new BlockExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.AssignmentExpression, typeof(AssignmentExpression), () => new AssignmentExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.IfStatementExpression, typeof(IfStatementExpression), () => new IfStatementExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.LocalDeclarationExpression, typeof(LocalDeclarationExpression), () => new LocalDeclarationExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.TypeExpression, typeof(TypeExpression), () => new TypeExpression()));
            //RegisterKnownType(new UserSerializer(PayloadType.ArrayCreationExpression, typeof(ArrayCreationExpression), () => new ArrayCreationExpression()));

            //FormModel、Drawing、数据源绑定相关
            //RegisterKnownType(new UserSerializer(PayloadType.ResourceImageSource, typeof(ResourceImageSource), () => new ResourceImageSource()));
            //RegisterKnownType(new UserSerializer(PayloadType.EntityDataSourceModel, typeof(EntityDataSourceModel), () => new EntityDataSourceModel()));
            //RegisterKnownType(new UserSerializer(PayloadType.ServiceDataSourceModel, typeof(ServiceDataSourceModel), () => new ServiceDataSourceModel()));
        }

        /// <summary>
        /// 注册可序列化的已知类型
        /// </summary>
        /// <returns>The sys known type.</returns>
        /// <param name="serializer">Serializer.</param>
        public static void RegisterKnownType(TypeSerializer serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (knownTypes.ContainsKey(serializer.TargetType))
                throw new ArgumentException("Already exsited type: " + serializer.TargetType.FullName);

            knownTypes.Add(serializer.TargetType, serializer);
            if (serializer.PayloadType == PayloadType.ExtKnownType)
                extKnownTypesIndexer.Add(serializer.ExtKnownTypeID, serializer);
            else
                sysKnownTypesIndexer.Add(serializer.PayloadType, serializer);
        }

        /// <summary>
        /// 序列化时根据目标类型获取相应的序列化实现
        /// </summary>
        /// <returns>The serializer.</returns>
        /// <param name="type">Type.</param>
        public static TypeSerializer GetSerializer(Type type)
        {
            TypeSerializer serializer = null;
            Type targetType = type;

            if (type.IsGenericType)
            {
                //注意：先尝试直接获取
                if (knownTypes.TryGetValue(targetType, out serializer))
                    return serializer;
                targetType = type.GetGenericTypeDefinition();
            }
            else if (type.IsArray)
                targetType = type.BaseType;

            knownTypes.TryGetValue(targetType, out serializer);
            return serializer;
        }

        /// <summary>
        /// 反序列化时根据PayloadType获取相应的系统已知类型的序列化实现
        /// </summary>
        /// <returns>The serializer.</returns>
        /// <param name="payloadType">Payload type.</param>
        public static TypeSerializer GetSerializer(PayloadType payloadType)
        {
            if (payloadType == PayloadType.ExtKnownType)
                throw new InvalidOperationException();

            return sysKnownTypesIndexer[payloadType];
        }

        /// <summary>
        /// 反序列化时根据ExtKnownTypeID获取相应的扩展已知类型的序列化实现
        /// </summary>
        /// <returns>The serializer.</returns>
        /// <param name="extKnownTypeID">Ext known type identifier.</param>
        public static TypeSerializer GetSerializer(ExtKnownTypeID extKnownTypeID)
        {
            return extKnownTypesIndexer[extKnownTypeID];
        }

        #region ----Static Serialize & Deserialize Methods----
        /// <summary>
        /// 序列化对象为字节数组
        /// </summary>
        /// <param name="obj">需要序列化的对象</param>
        /// <param name="compress">是否需要压缩</param>
        /// <param name="sa">加密提供者</param>
        /// <returns></returns>
        public static byte[] Serialize(object obj, bool compress, SymmetricAlgorithm sa)
        {
            //设置标志
            byte flag = 0;
            if (compress)
                flag = (byte)(flag | 1);
            if (sa != null)
                flag = (byte)(flag | 2);

            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte(flag);
                Stream stream = ms;
                if (sa != null)
                    stream = new CryptoStream(stream, sa.CreateEncryptor(), CryptoStreamMode.Write);

                BinSerializer cf = new BinSerializer(stream);
                try
                {
                    cf.Serialize(obj);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    cf.Clear();
                }

                stream.Close();
                data = ms.ToArray();
            }
            return data;
        }

        public static object Deserialize(byte[] data, SymmetricAlgorithm sa)
        {
            //读取标志
            byte flag = data[0];

            object result = null;
            using (MemoryStream ms = new MemoryStream(data, 1, data.Length - 1))
            {
                Stream stream = ms;
                if ((flag & 2) == 2)
                {
                    if (sa == null)
                        throw new ArgumentNullException(nameof(sa), "data is Encrypted, and SymmetricAlgorithm can not be null");
                    else
                        stream = new CryptoStream(stream, sa.CreateDecryptor(), CryptoStreamMode.Read);
                }

                BinSerializer cf = new BinSerializer(stream);
                try
                {
                    result = cf.Deserialize();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    cf.Clear();
                }

                stream.Close();
            }

            return result;
        }
        #endregion

        #endregion

        #region ====Fields====

        private Stream _stream;
        public Stream Stream
        {
            get { return _stream; }
        }

        /// <summary>
        /// 已经序列化或反序列化的对象实例列表
        /// </summary>
        private List<object> _objRefItems;

        #endregion

        #region ====Ctor & Init====
        private BinSerializer() { }

        public BinSerializer(Stream stream)
        {
            Init(stream);
        }

        public void Init(Stream stream)
        {
            _stream = stream;
        }
        #endregion

        #region ====Serialize & Deserialize Methods====

        public void Serialize(object obj, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            #region Null & DbNull
            if (obj == null)
            {
                //todo:处理Nullable类型
                _stream.WriteByte((byte)PayloadType.Null);
                return;
            }
            if (obj == DBNull.Value)
            {
                _stream.WriteByte((byte)PayloadType.DBNull);
                return;
            }
            #endregion

            Type type = obj.GetType();
            TypeSerializer serializer = GetSerializer(type);
            if (serializer == null)
            {
                Log.Error("未能找到序列化实现 类型:" + type.FullName);
                throw new SerializationException(SerializationError.CanNotFindSerializer, type.FullName);
            }

            #region 检查是否已经序列化过
            if (serializer.TargetType.IsClass && serializer.TargetType != typeof(string))
            {
                if (CheckSerialized(obj))
                    return;
            }
            #endregion

            //写入类型信息
            _stream.WriteByte((byte)serializer.PayloadType);
            //写入附加类型信息
            serializer.WriteAttachTypeInfo(this, type);

            //写入数据
            //判断是否引用类型，是则加入已序列化对象列表
            if (serializer.TargetType.IsClass && serializer.TargetType != typeof(string))
                AddToObjectRefs(obj);

            serializer.Write(this, obj);
        }

        public object Deserialize()
        {
            int res = _stream.ReadByte();
            if (res < 0)
                throw new SerializationException(SerializationError.NothingToRead);

            PayloadType payloadType = (PayloadType)res;
            if (payloadType == PayloadType.Null)
                return null;
            if (payloadType == PayloadType.DBNull)
                return DBNull.Value;
            if (payloadType == PayloadType.ObjectRef)
                return _objRefItems[(int)ReadUInt32()];

            TypeSerializer serializer;
            if (payloadType == PayloadType.ExtKnownType)
                serializer = GetSerializer(ReadExtKnownTypeID());
            else
                serializer = GetSerializer(payloadType);
            if (serializer == null)
                throw new SerializationException(SerializationError.CanNotFindSerializer, payloadType.ToString());

            //读取附加类型信息并创建实例
            if (serializer.Creator == null
                && payloadType != PayloadType.Array //非数组类型
                && serializer.GenericTypeCount <= 0) //非范型类型
            {
                return serializer.Read(this, null);
            }
            else //其他需要创建实例的类型
            {
                object result = null;
                if (payloadType == PayloadType.Array) //数组实例创建
                {
                    var elementType = ReadType();
                    int elementCount = VariantHelper.ReadInt32(_stream);
                    result = Array.CreateInstance(elementType, elementCount);
                }
                else if (serializer.GenericTypeCount > 0) //范型类型（引用及结构体）实例创建
                {
                    var genericTypes = new Type[serializer.GenericTypeCount];
                    for (int i = 0; i < serializer.GenericTypeCount; i++)
                    {
                        genericTypes[i] = ReadType();
                    }
                    var type = serializer.TargetType.MakeGenericType(genericTypes);
                    result = Activator.CreateInstance(type);
                }
                else
                {
                    result = serializer.Creator();
                }

                if (serializer.TargetType.IsClass && serializer.TargetType != typeof(string))
                    AddToObjectRefs(result); //引用类型加入已序列化列表
                serializer.Read(this, result);
                return result;
            }
        }

        #endregion

        #region ====Write & Read Methods====

        #region ----基本值类型----

        #region Boolean

        public void Write(bool value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        public bool ReadBoolean()
        {
            return _stream.ReadByte() == 1;
        }

        #endregion

        #region Byte

        public void Write(byte value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            _stream.WriteByte(value);
        }

        public byte ReadByte()
        {
            int res = _stream.ReadByte();
            if (res < 0)
                throw new SerializationException(SerializationError.NothingToRead);

            return (byte)res;
        }

        #endregion

        #region Char

        public void Write(char value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            Write((UInt16)value);
        }

        public Char ReadChar()
        {
            return (char)ReadUInt16();
        }

        #endregion

        #region Decimal

        public unsafe void Write(decimal value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            byte* p = (byte*)&value;
            for (int i = 0; i < 16; i++)
            {
                _stream.WriteByte(p[i]);
            }
        }

        public unsafe decimal ReadDecimal()
        {
            decimal d;
            byte* p = (byte*)&d;
            for (int i = 0; i < 16; i++)
            {
                p[i] = (byte)_stream.ReadByte();
            }
            return d;
        }

        #endregion

        #region Float

        public unsafe void Write(float value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            byte* p = (byte*)&value;
            for (int i = 0; i < 4; i++)
            {
                _stream.WriteByte(p[i]);
            }
        }

        public unsafe float ReadFloat()
        {
            float d;
            byte* p = (byte*)&d;
            for (int i = 0; i < 4; i++)
            {
                p[i] = (byte)_stream.ReadByte();
            }
            return d;
        }

        #endregion

        #region Double

        public unsafe void Write(double value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            byte* p = (byte*)&value;
            for (int i = 0; i < 8; i++)
            {
                _stream.WriteByte(p[i]);
            }
        }

        public unsafe double ReadDouble()
        {
            double d;
            byte* p = (byte*)&d;
            for (int i = 0; i < 8; i++)
            {
                p[i] = (byte)_stream.ReadByte();
            }
            return d;
        }

        #endregion

        #region Int16

        public unsafe void Write(Int16 value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            byte* p = (byte*)&value;
            _stream.WriteByte(p[0]);
            _stream.WriteByte(p[1]);
        }

        public unsafe Int16 ReadInt16()
        {
            Int16 v;
            byte* p = (byte*)&v;
            p[0] = (byte)_stream.ReadByte();
            p[1] = (byte)_stream.ReadByte();
            return v;
        }

        #endregion

        #region UInt16

        public unsafe void Write(UInt16 value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            byte* p = (byte*)&value;
            _stream.WriteByte(p[0]);
            _stream.WriteByte(p[1]);
        }

        public unsafe UInt16 ReadUInt16()
        {
            UInt16 v;
            byte* p = (byte*)&v;
            p[0] = (byte)_stream.ReadByte();
            p[1] = (byte)_stream.ReadByte();
            return v;
        }

        #endregion

        #region Int32

        public void Write(int value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            VariantHelper.WriteInt32(value, _stream);
        }

        public int ReadInt32()
        {
            return VariantHelper.ReadInt32(_stream);
        }

        #endregion

        #region UInt32

        public unsafe void Write(uint value, uint propIndex = 0, bool useVariant = true)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (useVariant)
            {
                VariantHelper.WriteUInt32(value, _stream);
            }
            else
            {
                byte* p = (byte*)&value;
                for (int i = 0; i < 4; i++)
                {
                    _stream.WriteByte(p[i]);
                }
            }
        }

        public unsafe uint ReadUInt32(bool useVariant = true)
        {
            if (useVariant)
                return VariantHelper.ReadUInt32(_stream);

            uint d;
            byte* p = (byte*)&d;
            for (int i = 0; i < 4; i++)
            {
                p[i] = (byte)_stream.ReadByte();
            }
            return d;
        }

        #endregion

        #region Int64

        public void Write(Int64 value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            VariantHelper.WriteInt64(value, _stream);
        }

        public Int64 ReadInt64()
        {
            return VariantHelper.ReadInt64(_stream);
        }

        #endregion

        #region UInt64

        public void Write(ulong value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            VariantHelper.WriteUInt64(value, _stream);
        }

        public ulong ReadUInt64()
        {
            return VariantHelper.ReadUInt64(_stream);
        }

        #endregion

        #region DateTime

        public void Write(DateTime value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            VariantHelper.WriteInt64(value.ToUniversalTime().Ticks, _stream);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(VariantHelper.ReadInt64(_stream), DateTimeKind.Utc).ToLocalTime();
        }

        #endregion

        #region String

        public void Write(string value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            StringSerializer.Write(this, value);
        }

        public string ReadString()
        {
            return StringSerializer.Read(this);
        }

        #endregion

        #region Guid

        public void Write(Guid value, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            unsafe
            {
                byte* p = (byte*)&value;
                for (int i = 0; i < 16; i++)
                {
                    _stream.WriteByte(p[i]);
                }
            }
        }

        public Guid ReadGuid()
        {
            Guid res;
            unsafe
            {
                byte* p = (byte*)&res;
                for (int i = 0; i < 16; i++)
                {
                    p[i] = (byte)_stream.ReadByte();
                }
            }
            return res;
        }

        #endregion

        #endregion

        #region ----Array Methods----

        #region BooleanArray

        public void Write(bool[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public bool[] ReadBoolArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            if (len == -2)
                return (bool[])_objRefItems[(int)ReadUInt32()];

            bool[] res = new bool[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadBoolean();
            }

            return res;
        }

        #endregion

        #region ByteArray

        public void Write(byte[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            _stream.Write(array, 0, array.Length);
        }

        public byte[] ReadByteArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (byte[])_objRefItems[(int)ReadUInt32()];

            byte[] res = new byte[len];
            AddToObjectRefs(res);
            _stream.Read(res, 0, len);

            return res;
        }

        #endregion

        #region DecimalArray

        public void Write(decimal[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public decimal[] ReadDecimalArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (decimal[])_objRefItems[(int)ReadUInt32()];

            decimal[] res = new decimal[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadDecimal();
            }

            return res;
        }

        #endregion

        #region FloatArray

        public void Write(float[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public float[] ReadFloatArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (float[])_objRefItems[(int)ReadUInt32()];

            float[] res = new float[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadFloat();
            }

            return res;
        }

        #endregion

        #region DoubleArray

        public void Write(Double[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public Double[] ReadDoubleArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (Double[])_objRefItems[(int)ReadUInt32()];

            Double[] res = new Double[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadDouble();
            }

            return res;
        }

        #endregion

        #region Int16Array

        public void Write(Int16[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public Int16[] ReadInt16Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (Int16[])_objRefItems[(int)ReadUInt32()];

            Int16[] res = new Int16[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadInt16();
            }

            return res;
        }

        #endregion

        #region UInt16Array

        public void Write(UInt16[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public UInt16[] ReadUInt16Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (UInt16[])_objRefItems[(int)ReadUInt32()];

            UInt16[] res = new UInt16[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadUInt16();
            }

            return res;
        }

        #endregion

        #region Int32Array

        public void Write(Int32[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public Int32[] ReadInt32Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (Int32[])_objRefItems[(int)ReadUInt32()];

            Int32[] res = new Int32[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadInt32();
            }

            return res;
        }

        #endregion

        #region UInt32Array

        public void Write(UInt32[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public UInt32[] ReadUInt32Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (UInt32[])_objRefItems[(int)ReadUInt32()];

            UInt32[] res = new UInt32[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadUInt32();
            }

            return res;
        }

        #endregion

        #region Int64Array

        public void Write(Int64[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public Int64[] ReadInt64Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (Int64[])_objRefItems[(int)ReadUInt32()];

            Int64[] res = new Int64[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadInt64();
            }

            return res;
        }

        #endregion

        #region UInt64Array

        public void Write(ulong[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public ulong[] ReadUInt64Array()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            if (len == -2)
                return (ulong[])_objRefItems[(int)ReadUInt32()];

            ulong[] res = new ulong[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadUInt64();
            }

            return res;
        }

        #endregion

        #region DateTimeArray

        public void Write(DateTime[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public DateTime[] ReadDateTimeArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (DateTime[])_objRefItems[(int)ReadUInt32()];

            DateTime[] res = new DateTime[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadDateTime();
            }

            return res;
        }

        #endregion

        #region StringArray

        public void Write(String[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public String[] ReadStringArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (String[])_objRefItems[(int)ReadUInt32()];

            String[] res = new String[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadString();
            }

            return res;
        }

        #endregion

        #region GuidArray

        public void Write(Guid[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Write(array[i]);
            }
        }

        public Guid[] ReadGuidArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (Guid[])_objRefItems[(int)ReadUInt32()];

            Guid[] res = new Guid[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = ReadGuid();
            }

            return res;
        }

        #endregion

        #region ObjectArray

        public void Write(object[] array, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(array))
                return;

            AddToObjectRefs(array);
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Serialize(array[i]);
            }
        }

        public object[] ReadObjectArray()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;
            else if (len == -2)
                return (object[])_objRefItems[(int)ReadUInt32()];

            object[] res = new object[len];
            AddToObjectRefs(res);
            for (int i = 0; i < len; i++)
            {
                res[i] = Deserialize();
            }

            return res;
        }

        #endregion

        #endregion

        #region ----Collection Methods----

        public void WriteList<T>(IList<T> list, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(list))
                return;

            AddToObjectRefs(list);
            Write(list.Count);
            WriteCollection(typeof(T), list.Count, (index) => list[index]);
        }

        public IList<T> ReadList<T>(Func<IList<T>> creator, Action<T> addAction)
        {
            int count = ReadInt32();
            if (count == -1)
                return null;
            else if (count == -2)
                return (IList<T>)_objRefItems[(int)ReadUInt32()];

            IList<T> list = creator == null ? new List<T>(count) : creator();
            AddToObjectRefs(list);
            ReadCollection(typeof(T), count, (index, value) => list.Add((T)value));
            return list;
        }

        public List<T> ReadList<T>()
        {
            return (List<T>)ReadList<T>(null, null);
        }

        public void WriteDictionary<TKey, TVal>(Dictionary<TKey, TVal> dic, uint propIndex = 0)
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            if (CheckNullOrSerialized(dic))
                return;

            AddToObjectRefs(dic);
            Write(dic.Count);
            foreach (TKey key in dic.Keys)
            {
                this.Serialize(key);
                this.Serialize(dic[key]);
            }
        }

        public Dictionary<TKey, TVal> ReadDictionary<TKey, TVal>(IEqualityComparer<TKey> comparer = null)
        {
            int count = ReadInt32();
            if (count == -1)
                return null;
            else if (count == -2)
                return (Dictionary<TKey, TVal>)_objRefItems[(int)ReadUInt32()];

            Dictionary<TKey, TVal> dic = null;
            if (comparer == null)
                dic = new Dictionary<TKey, TVal>();
            else
                dic = new Dictionary<TKey, TVal>(comparer);
            AddToObjectRefs(dic);

            for (int i = 0; i < count; i++)
            {
                dic.Add((TKey)this.Deserialize(), (TVal)this.Deserialize());
            }
            return dic;
        }

        #endregion

        #region ----实现了IBinSerializable接口的结构体----
        /// <summary>
        /// 写入实现了IBinSerializable接口的结构体类型
        /// </summary>
        public void WriteStruct<T>(T value, uint propIndex = 0) where T : struct, IBinSerializable
        {
            if (propIndex != 0)
                VariantHelper.WriteUInt32(propIndex, _stream);

            value.WriteObject(this);
        }

        /// <summary>
        /// 读取实现了IBinSerializable接口的结构体类型
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T ReadStruct<T>() where T : struct, IBinSerializable
        {
            var res = default(T);
            res.ReadObject(this);
            return res;
        }
        #endregion
        #endregion

        #region ====Clear Methods====

        public void Clear()
        {
            _objRefItems = null;
            _stream = null;
        }

        #endregion

        #region ====Read & Write Type====

        private ExtKnownTypeID ReadExtKnownTypeID()
        {
            ExtKnownTypeID id = new ExtKnownTypeID();
            id.AssemblyID = VariantHelper.ReadUInt32(_stream);
            id.TypeID = VariantHelper.ReadUInt32(_stream);
            return id;
        }

        public void WriteType(Type type)
        {
            //TypeFlag定义： 0 = 系统已知类型 1 = 扩展已知类型 2 = Any Object
            if (type == typeof(Object))
            {
                _stream.WriteByte((byte)2);
            }
            else
            {
                TypeSerializer serializer = GetSerializer(type);
                if (serializer == null)
                {
                    Log.Debug("未能找到序列化实现 类型:" + type.FullName);
                    throw new SerializationException(SerializationError.CanNotFindSerializer, type.FullName);
                }

                if (serializer.PayloadType == PayloadType.ExtKnownType)
                {
                    _stream.WriteByte((byte)1);
                }
                else
                {
                    _stream.WriteByte((byte)0);
                    _stream.WriteByte((byte)serializer.PayloadType);
                }
                //写入附加类型信息
                serializer.WriteAttachTypeInfo(this, type);
            }
        }

        public Type ReadType()
        {
            int byteRead = _stream.ReadByte();
            if (byteRead < 0)
                throw new SerializationException(SerializationError.NothingToRead);

            byte typeFlag = (byte)byteRead;
            switch (typeFlag)
            {
                case 0: //系统已知类型
                    {
                        byteRead = _stream.ReadByte();
                        if (byteRead < 0)
                            throw new SerializationException(SerializationError.NothingToRead);
                        PayloadType payloadType = (PayloadType)byteRead;
                        var serializer = GetSerializer(payloadType);
                        if (serializer == null)
                            throw new SerializationException(SerializationError.CanNotFindSerializer, payloadType.ToString());
                        if (serializer.PayloadType == PayloadType.Array)
                        {
                            var elementType = ReadType();
                            return elementType.MakeArrayType();
                        }
                        else if (serializer.GenericTypeCount > 0)
                        {
                            var genericTypes = new Type[serializer.GenericTypeCount];
                            for (int i = 0; i < serializer.GenericTypeCount; i++)
                            {
                                genericTypes[i] = ReadType();
                            }
                            return serializer.TargetType.MakeGenericType(genericTypes);
                        }
                        else
                        {
                            return serializer.TargetType;
                        }
                    }
                case 1:
                    {
                        var extID = ReadExtKnownTypeID();
                        var serializer = GetSerializer(extID);
                        if (serializer == null)
                            throw new SerializationException(SerializationError.CanNotFindSerializer, extID.ToString());
                        if (serializer.GenericTypeCount > 0)
                        {
                            var genericTypes = new Type[serializer.GenericTypeCount];
                            for (int i = 0; i < serializer.GenericTypeCount; i++)
                            {
                                genericTypes[i] = ReadType();
                            }
                            return serializer.TargetType.MakeGenericType(genericTypes);
                        }
                        else
                        {
                            return serializer.TargetType;
                        }
                    }
                case 2:
                    return typeof(Object);
                default:
                    throw new SerializationException(SerializationError.UnknownTypeFlag, typeFlag.ToString());
            }
        }

        #endregion

        #region ====Private Helper Methods====

        private void AddToObjectRefs(object obj)
        {
            if (_objRefItems == null)
                _objRefItems = new List<object>();

            _objRefItems.Add(obj);
        }

        private int IndexOfObjectRefs(object obj)
        {
            if (_objRefItems == null || _objRefItems.Count == 0)
                return -1;

            for (int i = 0; i < _objRefItems.Count; i++)
            {
                if (Object.ReferenceEquals(_objRefItems[i], obj))
                    return i;
            }

            return -1;
        }

        private bool CheckSerialized(object obj)
        {
            int index = IndexOfObjectRefs(obj);
            if (index == -1)
            {
                //注意：不能在这里AddToObjectRefs(obj);
                return false;
            }
            else
            {
                _stream.WriteByte((byte)PayloadType.ObjectRef);
                VariantHelper.WriteUInt32((uint)index, _stream);
                return true;
            }
        }

        private bool CheckNullOrSerialized(object obj)
        {
            if (obj == null)
            {
                Write(-1);
                return true;
            }

            int index = IndexOfObjectRefs(obj);
            if (index > -1)
            {
                Write(-2);
                Write((uint)index);
                return true;
            }

            //注意：不能在这里AddToObjectRefs(obj);
            return false;
        }

        #endregion

        #region ====Collection Write & Read Helper====

        internal void WriteCollection(Type elementType, int count, Func<int, object> elementGetter)
        {
            if (count == 0)
                return;

            //尝试获取elementType有没有相应的序列化实现存在
            var serializer = GetSerializer(elementType);
            if (serializer == null || (elementType.IsClass && elementType != typeof(string))) //引用类型，注意：elementType == typeof(Object)没有序列化实现
            {
                for (int i = 0; i < count; i++)
                {
                    Serialize(elementGetter(i));
                }
            }
            else //值类型
            {
                for (int i = 0; i < count; i++)
                {
                    serializer.Write(this, elementGetter(i));
                }
            }
        }

        internal void ReadCollection(Type elementType, int count, Action<int, object> elementSetter)
        {
            if (count == 0)
                return;

            var serializer = GetSerializer(elementType);
            if (serializer == null || (elementType.IsClass && elementType != typeof(string))) //元素为引用类型
            {
                for (int i = 0; i < count; i++)
                {
                    elementSetter(i, Deserialize());
                }
            }
            else //元素为值类型
            {
                if (serializer.GenericTypeCount > 0) //范型值类型
                {
                    for (int i = 0; i < count; i++)
                    {
                        object element = Activator.CreateInstance(elementType);
                        serializer.Read(this, element);
                        elementSetter(i, element);
                    }
                }
                else if (serializer.Creator != null) //带有构造器的值类型
                {
                    for (int i = 0; i < count; i++)
                    {
                        object element = serializer.Creator();
                        serializer.Read(this, element);
                        elementSetter(i, element);
                    }
                }
                else //其他值类型
                {
                    for (int i = 0; i < count; i++)
                    {
                        elementSetter(i, serializer.Read(this, null));
                    }
                }
            }
        }

        #endregion

    }
}

