using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Expressions;
using appbox.Models;

namespace appbox.Store
{
    /// <summary>
    /// 作为TableScan及IndexScan的基类
    /// </summary>
    public abstract class KVScan
    {
        #region ====Fields & Properties====
        //const int MaxTake = 10000;
        protected readonly ulong modelId;
        protected uint skip;
        protected uint take = uint.MaxValue; //MaxTake;

        /// <summary>
        /// 记录过滤条件表达式
        /// </summary>
        protected Expression filter;

        /// <summary>
        /// 用于EagerLoad导航属性 
        /// </summary>
        protected Includer rootIncluder;

        protected PartitionPredicates _partitions;
        /// <summary>
        /// 用于分区表指定分区的谓词
        /// </summary>
        public PartitionPredicates Partitions
        {
            get
            {
                if (_partitions == null)
                    _partitions = new PartitionPredicates();
                return _partitions;
            }
        }
        #endregion

        #region ====Ctor====
        protected KVScan(ulong modelId)
        {
            this.modelId = modelId;
        }
        #endregion

        #region ====TupleFieldExpression Methods====
        public KVFieldExpression GetString(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.String);
        }

        public KVFieldExpression GetGuid(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Guid);
        }

        public KVFieldExpression GetDateTime(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.DateTime);
        }

        public KVFieldExpression GetInt64(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Int64);
        }

        public KVFieldExpression GetUInt64(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.UInt64);
        }

        public KVFieldExpression GetInt32(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Int32);
        }

        public KVFieldExpression GetUInt32(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.UInt32);
        }

        public KVFieldExpression GetInt16(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Int16);
        }

        public KVFieldExpression GetUInt16(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.UInt16);
        }

        public KVFieldExpression GetByte(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Byte);
        }

        public KVFieldExpression GetFloat(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Float);
        }

        public KVFieldExpression GetDouble(ushort id)
        {
            return new KVFieldExpression(id, EntityFieldType.Double);
        }
        #endregion

        #region ====Include Methods====
        /// <summary>
        /// 用于Include EntityRef or EntitySet
        /// </summary>
        public Includer Include(EntityMemberType memberType, ushort memberId)
        {
            if (rootIncluder == null) rootIncluder = new Includer(modelId);
            return rootIncluder.Include(memberType, memberId);
        }

        /// <summary>
        /// 用于直接包含引用成员的字段，如t.Customer.Region.Name
        /// </summary>
        public Includer Include(string aliasName, ushort mid1, ushort mid2, ushort mid3 = 0)
        {
            if (rootIncluder == null) rootIncluder = new Includer(modelId);
            return rootIncluder.Include(aliasName, mid1, mid2, mid3);
        }

        protected async ValueTask LoadIncludesAsync(IList<Entity> list, ReadonlyTransaction txn)
        {
            if (rootIncluder == null || list == null) return;
            for (int i = 0; i < list.Count; i++) //TODO:并行执行
            {
                await rootIncluder.LoadAsync(list[i], txn);
            }
        }
        #endregion

        #region ====Partition Methods====
        protected async ValueTask< ulong[]> GetPartitions(byte appId, EntityModel model)
        {
            ulong[] parts;
            if (_partitions == null)
                parts = await PartitionPredicates.LoadPartitions(appId, model, null);
            else
                parts = await _partitions.GetPartitions(appId, model);
            return parts;
        }
        #endregion
    }
}
