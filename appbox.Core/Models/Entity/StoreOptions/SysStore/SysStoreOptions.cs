using System;
using System.Collections.Generic;
using System.Linq;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{

    /// <summary>
    /// 系统内置存储的选项，包括分区与二级索引
    /// </summary>
    public sealed class SysStoreOptions : IEntityStoreOptions
    {
        #region ====Fields & Properties====
        private const ushort MaxIndexId = 32; //2的5次方, 2bit Layer，1bit惟一标志

        private byte _devIndexIdSeq;
        private byte _usrIndexIdSeq;

        /// <summary>
        /// 是否Mvcc存储格式
        /// </summary>
        private EntityStoreType _storeType;

        /// <summary>
        /// 实体模型的分区键, Null表示不分区
        /// </summary>
        internal PartitionKey[] PartitionKeys { get; private set; }

        internal bool HasPartitionKeys => PartitionKeys != null && PartitionKeys.Length > 0;

        private List<EntityIndexModel> _indexes;
        /// <summary>
        /// 二级索引列表
        /// </summary>
        public List<EntityIndexModel> Indexes
        {
            get
            {
                if (_indexes == null)
                    _indexes = new List<EntityIndexModel>();
                return _indexes;
            }
        }
        public bool HasIndexes => _indexes != null && _indexes.Count > 0;
        IEnumerable<IndexModelBase> IEntityStoreOptions.Indexes => Indexes;

        /// <summary>
        /// 主键是否按时间倒序
        /// </summary>
        internal bool OrderByDesc { get; private set; }

        private uint? _oldSchemaVersion;
        internal uint OldSchemaVersion => _oldSchemaVersion == null ? SchemaVersion : _oldSchemaVersion.Value;
        internal uint SchemaVersion { get; set; }
        #endregion

        #region ====ShortPath for Store====

        /// <summary>
        /// 2bit RaftType + 1bit MvccFlag + 1bit OrderFlag
        /// </summary>
        internal byte TableFlags => (byte)(IdUtil.RAFT_TYPE_TABLE << IdUtil.RAFTGROUPID_FLAGS_TYPE_OFFSET
            | (_storeType == EntityStoreType.StoreWithMvcc ? 1 : 0) << IdUtil.RAFTGROUPID_FLAGS_MVCC_OFFSET
            | (OrderByDesc == true ? 1 : 0));

        /// <summary>
        /// 2bit RaftType + 1bit MvccFlag + 1bit OrderFlag(无用）
        /// </summary>
        internal byte IndexFlags => (byte)(IdUtil.RAFT_TYPE_INDEX << IdUtil.RAFTGROUPID_FLAGS_TYPE_OFFSET
            | (_storeType == EntityStoreType.StoreWithMvcc ? 1 : 0) << IdUtil.RAFTGROUPID_FLAGS_MVCC_OFFSET);
        #endregion

        #region ====Ctor====
        internal SysStoreOptions() { }

        internal SysStoreOptions(EntityStoreType storeType, bool orderByDesc)
        {
            _storeType = storeType;
            OrderByDesc = orderByDesc;
        }
        #endregion

        #region ====Design Methods====
        public void AcceptChanges()
        {
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

            _oldSchemaVersion = null;
        }

        /// <summary>
        /// 添加非空列、删除列、添加索引、删除索引后变更SchemaVersion
        /// </summary>
        /// <remarks>
        /// 由调用者判断是否新的模型
        /// </remarks>
        internal void ChangeSchemaVersion()
        {
            if (_oldSchemaVersion.HasValue) return;

            _oldSchemaVersion = SchemaVersion;
            SchemaVersion += 1;
        }

        internal void AddIndex(EntityModel owner, EntityIndexModel index)
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

            owner.ChangeSchemaVersion();
        }

        /// <summary>
        /// Only used for StoreInitiator
        /// </summary>
        internal void AddSysIndex(EntityModel owner, EntityIndexModel index, byte id)
        {
            owner.CheckDesignMode();
            owner.CheckOwner(index.Owner);

            index.InitIndexId(id);
            Indexes.Add(index);
        }


        /// <summary>
        /// 设置分区键，注意只有新建的模型可以设置
        /// </summary>
        internal void SetPartitionKeys(EntityModel owner, PartitionKey[] keys)
        {
            //TODO:验证每个PartitionKey
            owner.CheckDesignMode();
            if (owner.PersistentState != Data.PersistentState.Detached)
                throw new Exception("Only new entity model can set partition keys");

            PartitionKeys = keys;
            //TODO:更改对应的成员AllowNull = false
        }
        #endregion

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write((byte)_storeType, 1);
            bs.Write(OrderByDesc, 2);
            bs.Write(SchemaVersion, 3);

            //写入索引集
            if (HasIndexes)
            {
                bs.Write((uint)4);
                bs.Write(Indexes.Count);
                for (int i = 0; i < Indexes.Count; i++)
                {
                    bs.Serialize(Indexes[i]);
                }
            }
            //写入分区键
            if (HasPartitionKeys)
            {
                bs.Write((uint)5);
                bs.Write(PartitionKeys.Length);
                for (int i = 0; i < PartitionKeys.Length; i++)
                {
                    bs.Write(PartitionKeys[i].MemberId);
                    bs.Write(PartitionKeys[i].OrderByDesc);
                    bs.Write((byte)PartitionKeys[i].Rule);
                    bs.Write(PartitionKeys[i].RuleArgument);
                }
            }

            //if (DesignMode)
            //{
            bs.Write(_devIndexIdSeq, 6);
            bs.Write(_usrIndexIdSeq, 7);
            if (_oldSchemaVersion != null)
                bs.Write(_oldSchemaVersion.Value, 8);
            //}

            bs.Write((uint)0);
        }

        public void ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: _storeType = (EntityStoreType)bs.ReadByte(); break;
                    case 2: OrderByDesc = bs.ReadBoolean(); break;
                    case 3: SchemaVersion = bs.ReadUInt32(); break;
                    case 4:
                        {
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var idx = (EntityIndexModel)bs.Deserialize();
                                Indexes.Add(idx);
                            }
                        }
                        break;
                    case 5:
                        {
                            int count = bs.ReadInt32();
                            PartitionKeys = new PartitionKey[count];
                            for (int i = 0; i < count; i++)
                            {
                                PartitionKeys[i].MemberId = bs.ReadUInt16();
                                PartitionKeys[i].OrderByDesc = bs.ReadBoolean();
                                PartitionKeys[i].Rule = (PartitionKeyRule)bs.ReadByte();
                                PartitionKeys[i].RuleArgument = bs.ReadInt32();
                            }
                        }
                        break;
                    case 6: _devIndexIdSeq = bs.ReadByte(); break;
                    case 7: _usrIndexIdSeq = bs.ReadByte(); break;
                    case 8: _oldSchemaVersion = bs.ReadUInt32(); break;
                    case 0: break;
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(OrderByDesc));
            writer.WriteValue(OrderByDesc);

            writer.WritePropertyName(nameof(Indexes));
            if (!HasIndexes)
                writer.WriteRawValue("[]");
            else
                writer.Serialize(Indexes.Where(t => t.PersistentState != Data.PersistentState.Deleted).ToArray());

            //写入分区键集合
            writer.WritePropertyName(nameof(PartitionKeys));
            writer.WriteStartArray();
            if (HasPartitionKeys)
            {
                for (int i = 0; i < PartitionKeys.Length; i++)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("MemberId");
                    writer.WriteValue(PartitionKeys[i].MemberId);
                    writer.WritePropertyName("OrderByDesc");
                    writer.WriteValue(PartitionKeys[i].OrderByDesc);
                    writer.WritePropertyName("Rule");
                    writer.WriteValue(PartitionKeys[i].Rule);
                    writer.WritePropertyName("RuleArg");
                    writer.WriteValue(PartitionKeys[i].RuleArgument);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion
    }

}
