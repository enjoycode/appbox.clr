using System;
using System.Collections;
using System.Collections.Generic;

namespace appbox.Serialization
{
    internal sealed class ListSerializer : TypeSerializer
    {
       
        public ListSerializer() : base(PayloadType.List, typeof(List<>))
        {}

        public override void Write(BinSerializer bs, object instance)
        {
            var list = (IList)instance;
            //先写入元素个数
            VariantHelper.WriteInt32(list.Count, bs.Stream);
            //再写入各元素
            bs.WriteCollection(instance.GetType().GetGenericArguments()[0], list.Count, (index) => list[index]);
        }

        public override object Read(BinSerializer bs, object instance)
        {
            var list = (IList)instance;
            var elementType = instance.GetType().GetGenericArguments()[0];
            //注意：需要读取元素个数
            var count = VariantHelper.ReadInt32(bs.Stream);
            bs.ReadCollection(elementType, count, (index, value) => list.Add(value));
            return list;
        }

    }
}

