using System;

namespace appbox.Serialization
{

    /// <summary>
    /// 实现了IBinSerializable的类型的默认序列化实现
    /// </summary>
    public sealed class UserSerializer : TypeSerializer
    {

        public UserSerializer(PayloadType payloadType, Type sysType, Func<Object> creator, bool notWriteAttachInfo = false) :
            base(payloadType, sysType, creator, notWriteAttachInfo)
        {
            //if (creator == null)
            //    throw new ArgumentNullException(nameof(creator));
        }

        public UserSerializer(Type extType, uint assemblyID, uint typeID, Func<Object> creator) : base(extType, assemblyID, typeID, creator)
        {
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));
        }

        public override void Write(BinSerializer bs, object instance)
        {
            ((IBinSerializable)instance).WriteObject(bs);
        }

        public override object Read(BinSerializer bs, object instance)
        {
            ((IBinSerializable)instance).ReadObject(bs);
            return instance;
        }

    }

}

