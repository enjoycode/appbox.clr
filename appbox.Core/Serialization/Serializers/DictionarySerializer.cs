using System;
using System.Collections;
using System.Collections.Generic;

namespace appbox.Serialization
{
    sealed class DictionarySerializer : TypeSerializer
    {
        public DictionarySerializer() : base(PayloadType.Dictionary, typeof(Dictionary<,>))
        {
        }

        public override void Write(BinSerializer bs, object instance)
        {
            IDictionary dic = (IDictionary)instance;
            VariantHelper.WriteInt32(dic.Count, bs.Stream);
            foreach (var key in dic.Keys)
            {
                bs.Serialize(key);
                bs.Serialize(dic[key]);
            }
        }

        public override object Read(BinSerializer bs, object instance)
        {
            int count = VariantHelper.ReadInt32(bs.Stream);
            IDictionary dic = (IDictionary)instance;
            for (int i = 0; i < count; i++)
            {
                dic.Add(bs.Deserialize(), bs.Deserialize());
            }
            return instance;
        }
    }
}

