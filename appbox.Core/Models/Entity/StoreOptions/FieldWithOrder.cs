using System;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Models
{
    /// <summary>
    /// 作为主键或索引的字段，SysStore与SqlStore通用
    /// </summary>
    public struct FieldWithOrder : IJsonSerializable
    {
        public ushort MemberId;
        public bool OrderByDesc;

        public FieldWithOrder(ushort memberId, bool orderByDesc = false)
        {
            MemberId = memberId;
            OrderByDesc = orderByDesc;
        }

        #region ====Serialization====
        internal void WriteObject(BinSerializer bs)
        {
            bs.Write(MemberId, 1);
            bs.Write(OrderByDesc, 2);
            bs.Write((uint)0);
        }

        internal void ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: MemberId = bs.ReadUInt16(); break;
                    case 2: OrderByDesc = bs.ReadBoolean(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex} ");
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteNumber(nameof(MemberId), MemberId);
            writer.WriteBoolean(nameof(OrderByDesc), OrderByDesc);
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion
    }
}
