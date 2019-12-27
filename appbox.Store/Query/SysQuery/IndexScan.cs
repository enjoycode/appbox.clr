#if FUTURE

using System;
using System.Threading.Tasks;
using System.Linq;
using appbox.Models;
using appbox.Runtime;
using appbox.Server;
using appbox.Data;
using appbox.Expressions;
using System.Collections.Generic;

namespace appbox.Store
{
    /// <summary>
    /// 索引扫描
    /// </summary>
    public sealed class IndexScan : KVScan
    {
#region ====Fields & Properties====
        private readonly byte indexId;

        private IndexPredicates _keys;
        /// <summary>
        /// 用于指定索引键的谓词
        /// </summary>
        public IndexPredicates Keys
        {
            get
            {
                if (_keys == null) _keys = new IndexPredicates();
                return _keys;
            }
        }
#endregion

#region ====Ctor====
        public IndexScan(ulong modelId, byte indexId) : base(modelId)
        {
            this.indexId = indexId;
        }
#endregion

#region ====Skip & Take Methods====
        public IndexScan Skip(uint skip)
        {
            this.skip = skip;
            return this;
        }

        public IndexScan Take(uint take)
        {
            this.take = take;
            return this;
        }
#endregion

#region ====ToXXX Methods====
        /// <summary>
        /// 执行本地索引扫描，分区表与非分区表通用
        /// </summary>
        private async ValueTask<IScanResponse> ExecLocalIndexScanAsync(
            EntityIndexModel indexModel, ulong groupId, uint pskip, uint ptake, bool toIndexTarget)
        {
            //TODO:***将不能编入Key的谓词作为Filter条件
            //开始处理NativeApi所需参数
            IntPtr bkPtr;
            IntPtr ekPtr;
            int bkSize = KeyUtil.INDEXCF_PREFIX_SIZE;
            int ekSize = KeyUtil.INDEXCF_PREFIX_SIZE;
            IntPtr filterPtr = IntPtr.Zero;
            unsafe
            {
                int* varSizes = stackalloc int[indexModel.Fields.Length];
                if (_keys != null)
                    bkSize = ekSize = _keys.CalcKeySize(varSizes);

                byte* bk = stackalloc byte[bkSize];
                bkPtr = new IntPtr(bk);
                byte* ek = stackalloc byte[ekSize];
                ekPtr = new IntPtr(ek);

                if (_keys == null)
                {
                    KeyUtil.WriteIndexKeyPrefix(bk, groupId, indexModel.IndexId);
                    ekPtr = IntPtr.Zero;
                    ekSize = 0;
                }
                else
                {
                    _keys.WriteKeyRange(groupId, indexModel, bk, ek, varSizes);
                    if (_keys.IsOnlyEqual()) //只有相等性判断使用前缀匹配
                    {
                        ekPtr = IntPtr.Zero;
                        ekSize = 0;
                    }
                }
            }

            if (!Expression.IsNull(filter))
            {
                filterPtr = ModelStore.SerializeModel(filter, out _);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = groupId,
                BeginKeyPtr = bkPtr,
                BeginKeySize = new IntPtr(bkSize),
                EndKeyPtr = ekPtr,
                EndKeySize = new IntPtr(ekSize),
                FilterPtr = filterPtr,
                Skip = pskip,
                Take = ptake,
                DataCF = KeyUtil.INDEXCF_INDEX,
                ToIndexTarget = toIndexTarget
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            return await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
        }

        /// <summary>
        /// 执行查询并返回Entity[]
        /// </summary>
        /// <returns>返回值可能为null</returns>
        public async ValueTask<IList<Entity>> ToListAsync()
        {
            var app = await RuntimeContext.Current.GetApplicationModelAsync(IdUtil.GetAppIdFromModelId(modelId));
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            var indexModel = model.SysStoreOptions.Indexes.SingleOrDefault(t => t.IndexId == indexId);

            if (indexModel == null) throw new Exception("Index not exists.");
            if (indexModel.Global) throw new NotImplementedException();

            //先判断是否需要快照读事务 //TODO:跨分区也需要
            ReadonlyTransaction txn = rootIncluder == null ? null : new ReadonlyTransaction();

            if (model.SysStoreOptions.HasPartitionKeys)
            {
                //分区表先根据PartionPredicate查询出相关分区，再依次扫描
                ulong[] parts = await GetPartitions(app.StoreId, model);
                if (parts == null || parts.Length == 0)
                    return null;

                var list = new List<Entity>((int)(take <= 1000 ? take : 20));
                uint skipped = 0;
                uint taken = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    var partRes = await ExecLocalIndexScanAsync(indexModel, parts[i], skip - skipped, take - taken, true);
                    if (partRes != null)
                    {
                        partRes.ForEachRow((kp, ks, vp, vs) =>
                        {
                            list.Add(EntityStoreReader.ReadEntity(model, kp, ks, vp, vs));
                        });
                        skipped += partRes.Skipped;
                        taken += (uint)partRes.Length;
                        partRes.Dispose();
                        if (taken >= take)
                        {
                            break;
                        }
                    }
                }

                await LoadIncludesAsync(list, txn);
                return list;
            }
            else
            {
                ulong groupId = await EntityStore.GetOrCreateGlobalTablePartition(app, indexModel.Owner, IntPtr.Zero);
                if (groupId == 0)
                    return null;

                var res = await ExecLocalIndexScanAsync(indexModel, groupId, skip, take, true);
                if (res == null || res.Length == 0)
                    return null;

                var list = new Entity[res.Length];
                int rowIndex = 0;
                res.ForEachRow((kp, ks, vp, vs) =>
                {
                    list[rowIndex] = EntityStoreReader.ReadEntity(model, kp, ks, vp, vs);
                    rowIndex++;
                });
                res.Dispose();
                await LoadIncludesAsync(list, txn);
                return list;
            }
        }
#endregion
    }
}

#endif