using System;

namespace appbox.Serialization
{
    internal sealed class ArraySerializer : TypeSerializer
    {
        public ArraySerializer() : base(PayloadType.Array, typeof(Array))
        { }

        public override void Write(BinSerializer bs, object instance)
        {
            var array = (Array)instance;
            var elementType = array.GetType().GetElementType();

            //先写入元素个数
            VariantHelper.WriteInt32(array.Length, bs.Stream);
            //再写入各元素
            if (elementType == typeof(Byte))
                bs.Stream.Write((byte[])array, 0, array.Length);
            else
                bs.WriteCollection(elementType, array.Length, (index) => array.GetValue(index));
        }

        public override object Read(BinSerializer bs, object instance)
        {
            var array = (Array)instance;
            var elementType = array.GetType().GetElementType();

            //注意：不再需要读取元素个数，已由序列化器读过
            if (elementType == typeof(Byte))
                bs.Stream.Read((byte[])array, 0, array.Length);
            else
                bs.ReadCollection(elementType, array.Length, (index, value) => array.SetValue(value, index));
            return array;
        }
    }
}

