using System;
using System.Collections.Generic;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 第三方关系数据库存储选项，目前主要包含主键及索引设置
    /// </summary>
    public sealed class SqlStoreOptions : IEntityStoreOptions
    {

        /// <summary>
        /// 映射的DataStoreModel的标识
        /// </summary>
        public ulong StoreModelId { get; private set; }

        /// <summary>
        /// 仅用于设计时缓存
        /// </summary>
        internal string StoreName { get; set; }

        /// <summary>
        /// 主键成员
        /// </summary>
        public List<FieldWithOrder> PrimaryKeys { get; private set; }
        internal bool HasPrimaryKeys => PrimaryKeys != null && PrimaryKeys.Count > 0;
        internal bool PrimaryKeysHasChanged { get; private set; } = false;

        #region ====Ctor====
        internal SqlStoreOptions() { }

        internal SqlStoreOptions(ulong storeModelId)
        {
            StoreModelId = storeModelId;
        }
        #endregion

        #region ====Design Methods====
        public void AcceptChanges()
        {
            PrimaryKeysHasChanged = false;
        }

        internal void SetPrimaryKeys(EntityModel owner, List<FieldWithOrder> fields)
        {
            owner.CheckDesignMode();
            PrimaryKeys = fields;
            PrimaryKeysHasChanged = true;
        }
        #endregion

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write(StoreModelId, 1);

            if (HasPrimaryKeys)
            {
                bs.Write(2u);
                bs.Write(PrimaryKeys.Count);
                for (int i = 0; i < PrimaryKeys.Count; i++)
                {
                    PrimaryKeys[i].WriteObject(bs);
                }
            }
            bs.Write(PrimaryKeysHasChanged, 3);

            bs.Write(0u);
        }

        public void ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1:
                        StoreModelId = bs.ReadUInt64(); break;
                    case 2:
                        {
                            if (PrimaryKeys == null)
                                PrimaryKeys = new List<FieldWithOrder>();
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var f = new FieldWithOrder();
                                f.ReadObject(bs);
                                PrimaryKeys.Add(f);
                            }
                        }
                        break;
                    case 3:
                        PrimaryKeysHasChanged = bs.ReadBoolean(); break;
                    //case 4:
                    //    {
                    //        int count = bs.ReadInt32();
                    //        for (int i = 0; i < count; i++)
                    //        {
                    //            var mv = new SqlIndex();
                    //            mv.ReadObject(bs);
                    //            this.Indexes.Add(mv);
                    //        }
                    //    }
                    //    break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex} ");
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(StoreName));
            writer.WriteValue(StoreName);

            writer.WritePropertyName(nameof(PrimaryKeys));
            if (HasPrimaryKeys)
                writer.Serialize(PrimaryKeys);
            else //null发送[]方便前端
                writer.WriteRawValue("[]");
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
