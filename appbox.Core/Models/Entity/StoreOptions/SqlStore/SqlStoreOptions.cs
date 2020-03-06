using System;
using System.Linq;
using System.Collections.Generic;
using appbox.Serialization;
using System.Text.Json;

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

        private DataStoreModel _dataStoreModel_cached;
        /// <summary>
        /// 仅用于缓存
        /// </summary>
        internal DataStoreModel DataStoreModel
        {
            get
            {
                if (_dataStoreModel_cached == null) //仅在运行时可能为null
                    _dataStoreModel_cached = Runtime.RuntimeContext.Current.GetModelAsync<DataStoreModel>(StoreModelId).Result;
                return _dataStoreModel_cached;
            }
            set
            {
                //仅用于设计时
                _dataStoreModel_cached = value;
            }
        }
        /// <summary>
        /// 仅用于缓存，因向前端序列化需要用到
        /// </summary>
        internal EntityModel Owner { get; set; }

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

            //TODO:同AddMember获取当前Layer
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

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString("StoreName", DataStoreModel.Name);
            writer.WriteNumber("StoreKind", (int)DataStoreModel.Kind);

            writer.WritePropertyName(nameof(PrimaryKeys));
            if (HasPrimaryKeys)
            {
                //注意: 需要将主键成员中是EntityRef's的外键成员转换为EntityRef成员Id,以方便前端显示
                //eg: OrderId => Order，否则前端会找不到OrderId成员无法显示相应的名称
                var pks = new List<FieldWithOrder>(PrimaryKeys);
                var refs = new List<FieldWithOrder>();
                for (int i = pks.Count - 1; i >= 0; i--)
                {
                    var memberModel = (DataFieldModel)Owner.GetMember(pks[i].MemberId, true);
                    var refMemberModel = memberModel.GetEntityRefModelByForeignKey();
                    if (refMemberModel != null && refs.FindIndex(t => t.MemberId == refMemberModel.MemberId) < 0)
                    {
                        refs.Insert(0, new FieldWithOrder
                        {
                            MemberId = refMemberModel.MemberId,
                            OrderByDesc = pks[i].OrderByDesc
                        });
                        pks.RemoveAt(i);
                    }
                }
                pks.AddRange(refs);
                writer.Serialize(pks, objrefs);
            }
            else //null发送[]方便前端
            {
                writer.WriteEmptyArray();
            }

            writer.WritePropertyName(nameof(Indexes));
            if (!HasIndexes)
                writer.WriteEmptyArray();
            else
                writer.Serialize(Indexes.Where(t => t.PersistentState != Data.PersistentState.Deleted).ToArray(), objrefs);
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion

        #region ====导入方法====
        void IEntityStoreOptions.Import(EntityModel owner)
        {
            if (!HasIndexes) return;

            for (int i = 0; i < _indexes.Count; i++)
            {
                _indexes[i].Import(owner);
            }
        }

        void IEntityStoreOptions.UpdateFrom(EntityModel owner, IEntityStoreOptions other)
        {
            //TODO:支持主键变更后，更新主键设置
            var from = (SqlStoreOptions)other;
            if (from != null && from.HasIndexes)
            {
                var indexComparer = new SqlIndexComparer();
                //注意顺序:删除的 then 更新的 then 新建的
                var removedIndexes = Indexes.Except(from.Indexes, indexComparer);
                foreach (var removedIndex in removedIndexes)
                {
                    removedIndex.MarkDeleted();
                }
                var otherIndexes = Indexes.Intersect(from.Indexes, indexComparer);
                foreach (var otherIndex in otherIndexes)
                {
                    otherIndex.UpdateFrom(from.Indexes.Single(t => t.IndexId == otherIndex.IndexId));
                }
                var addedIndexes = from.Indexes.Except(Indexes, indexComparer);
                foreach (var addedIndex in addedIndexes)
                {
                    addedIndex.Import(owner);
                    Indexes.Add(addedIndex);
                    owner.OnPropertyChanged();
                }
            }
            else
            {
                if (HasIndexes)
                {
                    for (int i = 0; i < _indexes.Count; i++)
                    {
                        _indexes[i].MarkDeleted();
                    }
                }
            }

            //同步索引计数器
            _devIndexIdSeq = Math.Max(_devIndexIdSeq, from._devIndexIdSeq);
            //_usrIndexIdSeq = Math.Max(_usrIndexIdSeq, from._usrIndexIdSeq);
        }

        private class SqlIndexComparer : IEqualityComparer<SqlIndexModel>
        {
            public bool Equals(SqlIndexModel x, SqlIndexModel y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.IndexId == y.IndexId;
            }

            public int GetHashCode(SqlIndexModel obj)
            {
                return obj == null ? 0 : obj.IndexId.GetHashCode();
            }
        }
        #endregion
    }
}
