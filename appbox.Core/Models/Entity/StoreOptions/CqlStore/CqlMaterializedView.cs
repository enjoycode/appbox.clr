using System;
using appbox.Data;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 表存储的物化视图定义
    /// 注意：暂列为所有(*), 条件皆为IS NOT NULL
    /// </summary>
    public sealed class CqlMaterializedView
    {

        #region ====Fields & Properties====
        public string Name { get; private set; }

        public CqlPrimaryKey PrimaryKey { get; private set; } = new CqlPrimaryKey();

        public PersistentState PersistentState { get; private set; }
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal CqlMaterializedView() { }

        public CqlMaterializedView(string name)
        {
            Name = name;
        }
        #endregion

        #region ====设计时方法====
        internal void Validate(CqlStoreOptions owner)
        {
            // 物化视图主键限制: http://cassandra.apache.org/doc/latest/cql/mvs.html#create-materialized-view
            // 1. it must contain all the primary key columns of the base table.
            // 2. it can only contain a single column that is not a primary key column in the base table.

            //检查规则1
            bool hasAllBaseTablePK = true;
            for (int i = 0; i < owner.PrimaryKey.PartitionKeys.Length; i++)
            {
                if (!PrimaryKey.IsPrimaryKey(owner.PrimaryKey.PartitionKeys[i]))
                {
                    hasAllBaseTablePK = false;
                    break;
                }
            }
            if (hasAllBaseTablePK && owner.PrimaryKey.ClusteringColumns != null)
            {
                for (int i = 0; i < owner.PrimaryKey.ClusteringColumns.Length; i++)
                {
                    if (!PrimaryKey.IsPrimaryKey(owner.PrimaryKey.ClusteringColumns[i].MemberId))
                    {
                        hasAllBaseTablePK = false;
                        break;
                    }
                }
            }
            if (!hasAllBaseTablePK)
                throw new Exception("物化视图主键必须包含原表所有主键列");

            //检查规则2
            int nonPkCount = 0;
            for (int i = 0; i < PrimaryKey.PartitionKeys.Length; i++)
            {
                if (!owner.PrimaryKey.IsPrimaryKey(PrimaryKey.PartitionKeys[i]))
                {
                    nonPkCount++;
                    if (nonPkCount > 1) throw new Exception("物化视图主键只能包含一列原表的非主键列");
                }
            }
            if (PrimaryKey.ClusteringColumns != null)
            {
                for (int i = 0; i < PrimaryKey.ClusteringColumns.Length; i++)
                {
                    if (!owner.PrimaryKey.IsPrimaryKey(PrimaryKey.ClusteringColumns[i].MemberId))
                    {
                        nonPkCount++;
                        if (nonPkCount > 1) throw new Exception("物化视图主键只能包含一列原表的非主键列");
                    }
                }
            }
        }

        internal void MarkDelete()
        {
            PersistentState = PersistentState.Deleted;
        }

        internal void AcceptChanges()
        {
            PersistentState = PersistentState.Unchanged;
        }
        #endregion

        #region ====序列化方法====
        internal void WriteObject(BinSerializer bs)
        {
            bs.Write(Name, 1);

            bs.Write((uint)2);
            PrimaryKey.WriteObject(bs);

            bs.Write((byte)PersistentState, 3);

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
                    case 1: Name = bs.ReadString(); break;
                    case 2: PrimaryKey.ReadObject(bs); break;
                    case 3: PersistentState = (PersistentState)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception(string.Format("Deserialize_ObjectUnknownFieldIndex: {0} at {1} ", GetType().Name, propIndex));
                }
            } while (propIndex != 0);
        }

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            writer.WriteString(nameof(Name), Name);
            PrimaryKey.WriteToJson(writer); //TODO: check need property name
        }
        #endregion

        #region ====导入方法====
        internal void Import()
        {
            PersistentState = PersistentState.Detached;
        }
        #endregion
    }
}
