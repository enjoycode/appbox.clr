using System;

namespace appbox.Serialization
{

    //todo:暂全部序列化为Int32

    /// <summary>
    /// 枚举序列化实现
    /// </summary>
    public sealed class EnumSerializer : TypeSerializer
    {
        public EnumSerializer(Type enumType, uint assemblyID, uint typeID) : base(enumType, assemblyID, typeID)
        {}

        public override void Write(BinSerializer bs, object instance)
        {
            VariantHelper.WriteInt32(Convert.ToInt32(instance), bs.Stream); //todo: fix Convert.ToInt32
        }

        public override object Read(BinSerializer bs, object instance)
        {
            int value = VariantHelper.ReadInt32(bs.Stream);
            return Enum.ToObject(this.TargetType, value);
        }

    }
}

