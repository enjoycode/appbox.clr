using System;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 作为主键或索引的字段
    /// </summary>
    public struct SqlField : IJsonSerializable
    {
        public ushort MemberId;
        public bool OrderByDesc;

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
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(MemberId));
            writer.WriteValue(MemberId);
            writer.WritePropertyName(nameof(OrderByDesc));
            writer.WriteValue(OrderByDesc);
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
