using System;

namespace appbox.Serialization
{
    /// <summary>
    /// 某一类型的二进制序列化实现
    /// </summary>
    public abstract class TypeSerializer
    {
        /// <summary>
        /// 用于实体DataField成员不写入附加范型信息
        /// </summary>
        private bool notWriteAttachInfo = false;

        public PayloadType PayloadType { get; private set; }

        /// <summary>
        /// 对应的类型，范型为对应的GenericTypeDefinition
        /// </summary>
        /// <value>The type of the target.</value>
        public Type TargetType { get; private set; }

        /// <summary>
        /// 范型类型的范型参数个数
        /// </summary>
        /// <value>The generic type count.</value>
        public int GenericTypeCount { get; private set; }

        private ExtKnownTypeID extKnownTypeID;
        /// <summary>
        /// 扩展类型的标识
        /// </summary>
        /// <value>The ext known type identifier.</value>
        public ExtKnownTypeID ExtKnownTypeID { get { return extKnownTypeID; } }

        /// <summary>
        /// 引用类型的实例构造器，数组及范型类型除外
        /// </summary>
        /// <value>The creator.</value>
        public Func<Object> Creator { get; private set; }

        /// <summary>
        /// 系统已知类型序列化实现构造
        /// </summary>
        /// <param name="payloadType">Payload type.</param>
        /// <param name="sysType">Target type.</param>
        /// <param name="creator">Creator.</param>
        public TypeSerializer(PayloadType payloadType, Type sysType, Func<Object> creator = null, bool notWriteAttachInfo = false)
        {
            if (payloadType == PayloadType.ExtKnownType)
                throw new ArgumentException("payloadType can not be ExtKnownType", nameof(payloadType));

            this.notWriteAttachInfo = notWriteAttachInfo;
            this.PayloadType = payloadType;
            this.TargetType = sysType;
            this.Creator = creator;

            if (sysType.IsGenericType && !notWriteAttachInfo)
            {
                if (!sysType.IsGenericTypeDefinition)
                    throw new ArgumentException("targetType must be a GenericTypeDefinition", nameof(sysType));
                this.GenericTypeCount = sysType.GetGenericArguments().Length;
            }
            else
            {
                this.GenericTypeCount = 0;
            }
        }

        /// <summary>
        /// 扩展已知类型序列化实现构造
        /// </summary>
        /// <param name="extType">Ext type.</param>
        /// <param name="assemblyID">Assembly identifier.</param>
        /// <param name="typeID">Type identifier.</param>
        /// <param name="creator">Creator.</param>
        public TypeSerializer(Type extType, uint assemblyID, uint typeID, Func<Object> creator = null)
        {
            this.PayloadType = PayloadType.ExtKnownType;
            this.TargetType = extType;
            this.Creator = creator;
            this.extKnownTypeID.AssemblyID = assemblyID;
            this.extKnownTypeID.TypeID = typeID;

            if (extType.IsGenericType)
            {
                if (!extType.IsGenericTypeDefinition)
                    throw new ArgumentException("targetType must be a GenericTypeDefinition", nameof(extType));
                this.GenericTypeCount = extType.GetGenericArguments().Length;
            }
            else
            {
                this.GenericTypeCount = 0;
            }
        }

        /// <summary>
        /// Write data to BinSerializaer
        /// </summary>
        /// <param name="bs">Bs.</param>
        /// <param name="instance">None null instance</param>
        public abstract void Write(BinSerializer bs, object instance);

        /// <summary>
        /// 1.用于非范型值类型的反序列化，由实现自行创建实例
        /// 2.用于引用类型及范型值类型的反序列化，由序列化器创建实例
        /// </summary>
        /// <param name="bs">Bs.</param>
        /// <param name="instance">用于引用类型及范型值类型的反序列化，由序列化器创建的实例</param>
        public abstract object Read(BinSerializer bs, object instance);

        internal void WriteAttachTypeInfo(BinSerializer bs, Type type)
        {
            if (this.notWriteAttachInfo)
                return;

            if (this.PayloadType == PayloadType.Array)
            {
                Type elementType = type.GetElementType();
                bs.WriteType(elementType);
            }
            else
            {
                if (this.PayloadType == PayloadType.ExtKnownType) //扩展类型先写入扩展类型标识
                {
                    VariantHelper.WriteUInt32(this.extKnownTypeID.AssemblyID, bs.Stream);
                    VariantHelper.WriteUInt32(this.extKnownTypeID.TypeID, bs.Stream);
                }

                //再判断是否范型，是则写入范型各参数的类型信息
                if (this.GenericTypeCount > 0)
                {
                    var genericTypes = type.GetGenericArguments();
                    for (int i = 0; i < genericTypes.Length; i++)
                    {
                        bs.WriteType(genericTypes[i]);
                    }
                }
            }
        }

    }
}

