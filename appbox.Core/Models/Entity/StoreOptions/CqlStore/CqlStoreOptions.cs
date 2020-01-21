using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using appbox.Data;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 表存储相关选项，如主键、二级索引、物化视图等
    /// /// </summary>
    public sealed class CqlStoreOptions : IEntityStoreOptions, IJsonSerializable, IBinSerializable
    {
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

        public CqlPrimaryKey PrimaryKey { get; private set; } = new CqlPrimaryKey();

        private List<CqlMaterializedView> _materializedViews = null;
        public List<CqlMaterializedView> MaterializedViews
        {
            get
            {
                if (_materializedViews == null)
                    _materializedViews = new List<CqlMaterializedView>();
                return _materializedViews;
            }
        }

        public bool HasMaterializedView
        {
            get { return _materializedViews != null && _materializedViews.Count > 0; }
        }

        public bool HasIndexes => false; //TODO: fix

        public IEnumerable<IndexModelBase> Indexes => throw new NotImplementedException();

        #region ====Ctor====
        internal CqlStoreOptions() { }

        internal CqlStoreOptions(ulong storeModelId)
        {
            StoreModelId = storeModelId;
        }
        #endregion

        #region ====设计时方法====
        internal void Validate()
        {
            PrimaryKey.Validate();
            if (HasMaterializedView)
            {
                for (int i = 0; i < _materializedViews.Count; i++)
                {
                    _materializedViews[i].Validate(this);
                }
            }
        }

        public void AcceptChanges()
        {
            if (HasMaterializedView)
            {
                for (int i = _materializedViews.Count - 1; i >= 0; i--)
                {
                    switch (_materializedViews[i].PersistentState)
                    {
                        case PersistentState.Deleted:
                            _materializedViews.RemoveAt(i); break;
                        case PersistentState.Modified:
                        case PersistentState.Detached:
                            _materializedViews[i].AcceptChanges(); break;
                    }
                }
            }
        }
        #endregion

        #region ====序列化相关====
        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteObject(BinSerializer bs)
        {
            bs.Write(1u);
            PrimaryKey.WriteObject(bs);

            bs.Write(StoreModelId, 2u);

            if (HasMaterializedView)
            {
                bs.Write(5u);
                bs.Write(_materializedViews.Count);
                for (int i = 0; i < _materializedViews.Count; i++)
                {
                    _materializedViews[i].WriteObject(bs);
                }
            }

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
                        PrimaryKey.ReadObject(bs); break;
                    case 2: StoreModelId = bs.ReadUInt64(); break;
                    case 5:
                        {
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var mv = new CqlMaterializedView();
                                mv.ReadObject(bs);
                                MaterializedViews.Add(mv);
                            }
                        }
                        break;
                    case 0: break;
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString("StoreName", DataStoreModel.Name);
            writer.WriteNumber("StoreKind", (int)DataStoreModel.Kind);

            PrimaryKey.WriteToJson(writer); //不需要属性名

            writer.WritePropertyName("MaterializedViews");
            writer.WriteStartArray();
            if (HasMaterializedView)
            {
                for (int i = 0; i < _materializedViews.Count; i++)
                {
                    if (_materializedViews[i].PersistentState != PersistentState.Deleted)
                    {
                        writer.WriteStartObject();
                        _materializedViews[i].WriteToJson(writer);
                        writer.WriteEndObject();
                    }
                }
            }

            writer.WriteEndArray();
        }
        #endregion

        #region ====导入方法====
        //void IEntityStoreOptions.Import()
        //{
        //    if (HasMaterializedView)
        //    {
        //        for (int i = 0; i < _materializedViews.Count; i++)
        //        {
        //            _materializedViews[i].Import();
        //        }
        //    }
        //}

        //void IEntityStoreOptions.UpdateFrom(IEntityStoreOptions other)
        //{
        //    var from = (CqlStoreOptions)other;
        //    if (from != null && from.HasMaterializedView)
        //    {
        //        var mvComparer = new ViewComparer();
        //        var addedViews = from.MaterializedViews.Except(MaterializedViews, mvComparer);
        //        foreach (var addedView in addedViews)
        //        {
        //            addedView.Import();
        //            this.MaterializedViews.Add(addedView);
        //        }
        //        var removedViews = MaterializedViews.Except(from.MaterializedViews, mvComparer);
        //        foreach (var removedView in removedViews)
        //        {
        //            removedView.MarkDelete();
        //        }
        //        // 暂不需要更新
        //        // var otherViews = this.MaterializedViews.Intersect(from.MaterializedViews, mvComparer);
        //        // foreach (var otherView in otherViews)
        //        // {
        //        //     otherView.UpdateFrom(from.MaterializedViews.Single(t => t.Name == otherView.Name));
        //        // }
        //    }
        //    else
        //    {
        //        if (HasMaterializedView)
        //        {
        //            for (int i = 0; i < _materializedViews.Count; i++)
        //            {
        //                _materializedViews[i].MarkDelete();
        //            }
        //        }
        //    }
        //}

        private class ViewComparer : IEqualityComparer<CqlMaterializedView>
        {
            public bool Equals(CqlMaterializedView x, CqlMaterializedView y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(CqlMaterializedView obj)
            {
                return obj == null ? 0 : obj.Name.GetHashCode();
            }
        }
        #endregion
    }
}
