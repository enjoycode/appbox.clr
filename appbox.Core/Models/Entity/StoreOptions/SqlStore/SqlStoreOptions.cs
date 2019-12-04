using System;
using System.Linq;
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
        private const ushort MaxIndexId = 32; //2的5次方, 2bit Layer，1bit惟一标志

        private byte _devIndexIdSeq;
        private byte _usrIndexIdSeq;

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

        private List<SqlIndexModel> _indexes;
        /// <summary>
        /// 二级索引列表
        /// </summary>
        internal List<SqlIndexModel> Indexes
        {
            get
            {
                if (_indexes == null)
                    _indexes = new List<SqlIndexModel>();
                return _indexes;
            }
        }
        public bool HasIndexes => _indexes != null && _indexes.Count > 0;
        IEnumerable<IndexModelBase> IEntityStoreOptions.Indexes => Indexes;

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

            if (HasIndexes)
            {
                for (int i = _indexes.Count - 1; i >= 0; i--)
                {
                    if (_indexes[i].PersistentState == Data.PersistentState.Deleted)
                        _indexes.RemoveAt(i);
                    else
                        _indexes[i].AcceptChanges();
                }
            }
        }

        internal void SetPrimaryKeys(EntityModel owner, List<FieldWithOrder> fields)
        {
            owner.CheckDesignMode();
            PrimaryKeys = fields;
            PrimaryKeysHasChanged = true;
            owner.OnPropertyChanged();
        }

        internal void AddIndex(EntityModel owner, SqlIndexModel index)
        {
            owner.CheckDesignMode();
            owner.CheckOwner(index.Owner);

            //TODO:同上AddMember
            var layer = ModelLayer.DEV;
            var seq = layer == ModelLayer.DEV ? ++_devIndexIdSeq : ++_usrIndexIdSeq;
            if (seq >= MaxIndexId) //TODO:找空的
                throw new Exception("IndexId out of range");

            byte indexId = (byte)(seq << 2 | (byte)layer);
            if (index.Unique)
                indexId |= 1 << IdUtil.INDEXID_UNIQUE_OFFSET;
            index.InitIndexId(indexId);
            Indexes.Add(index);
            owner.OnPropertyChanged();
        }
        #endregion

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write(StoreModelId, 1);

            //写入主键
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

            //写入索引
            if (HasIndexes)
            {
                bs.Write(4u);
                bs.Write(Indexes.Count);
                for (int i = 0; i < Indexes.Count; i++)
                {
                    bs.Serialize(Indexes[i]);
                }
            }

            //if (DesignMode)
            //{
            bs.Write(_devIndexIdSeq, 6);
            bs.Write(_usrIndexIdSeq, 7);
            //}

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
                    case 4:
                        {
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var idx = (SqlIndexModel)bs.Deserialize();
                                Indexes.Add(idx);
                            }
                        }
                        break;
                    case 6: _devIndexIdSeq = bs.ReadByte(); break;
                    case 7: _usrIndexIdSeq = bs.ReadByte(); break;
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

            writer.WritePropertyName(nameof(Indexes));
            if (!HasIndexes)
                writer.WriteRawValue("[]");
            else
                writer.Serialize(Indexes.Where(t => t.PersistentState != Data.PersistentState.Deleted).ToArray());
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion
    }
}
