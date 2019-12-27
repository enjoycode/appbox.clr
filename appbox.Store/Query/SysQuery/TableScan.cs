#if FUTURE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Expressions;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 表扫描
    /// </summary>
    public sealed class TableScan : KVScan
    {
#region ====Ctor====
        public TableScan(ulong modelId) : base(modelId) { }
#endregion

#region ====Skip & Take Methods====
        public TableScan Skip(uint skip)
        {
            this.skip = skip;
            return this;
        }

        public TableScan Take(uint take)
        {
            this.take = take;
            return this;
        }
#endregion

#region ====Filter Methods====
        public TableScan Filter(Expression conditon)
        {
            //t.GetString(1) == "Rick" && t.GetInt32(2) > 20
            filter = conditon;
            return this;
        }
#endregion

#region ====ToXXX Methods====
        /// <summary>
        /// 执行分区扫描，全局表与分区表通用
        /// </summary>
        private async ValueTask<IScanResponse> ExecPartScanAsync(ulong groupId, uint pskip, uint ptake)
        {
            //TODO:暂重复序列化，考虑复制序列化结果
            IntPtr filterPtr = IntPtr.Zero;
            if (!Expression.IsNull(filter))
            {
                filterPtr = ModelStore.SerializeModel(filter, out _);
            }

            //开始处理NativeApi所需参数
            //暂没有CreateTime谓词使用前缀匹配方式
            IntPtr keyPtr;
            int keySize = KeyUtil.ENTITY_KEY_SIZE - 10;
            unsafe
            {
                byte* bk = stackalloc byte[keySize];
                EntityId.WriteRaftGroupId(bk, groupId);
                keyPtr = new IntPtr(bk);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = groupId,
                BeginKeyPtr = keyPtr,
                BeginKeySize = new IntPtr(keySize),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = filterPtr,
                Skip = pskip,
                Take = ptake,
                DataCF = -1,
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

            //先判断是否需要快照读事务 //TODO:跨分区也需要
            ReadonlyTransaction txn = rootIncluder == null ? null : new ReadonlyTransaction();

            //根据是否分区执行不同的查询
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
                    var partRes = await ExecPartScanAsync(parts[i], skip - skipped, take - taken);
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
            else //----------------------------------------------
            {
                ulong groupId = await EntityStore.GetOrCreateGlobalTablePartition(app, model, IntPtr.Zero);
                if (groupId == 0)
                    return null;
                var res = await ExecPartScanAsync(groupId, skip, take);
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

        /// <summary>
        /// 加载全表为树形结构
        /// </summary>
        public async ValueTask<EntityList> ToTreeListAsync(ushort setMemberId) //TODO:入参排序表达式
        {
            filter = null; //TODO:暂忽略过滤器
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            if (model == null)
                throw new Exception($"EntityModel[{modelId}] not exists.");
            if (!(model.GetMember(setMemberId, true) is EntitySetModel setModel))
                throw new ArgumentException("Must assign EntitySet member id", nameof(setMemberId));
            if (setModel.RefModelId != model.Id)
                throw new ArgumentException("Can't be a tree");
            EntityRefModel refModel = (EntityRefModel)setModel.Owner.GetMember(setModel.RefMemberId, true);

            var list = await ToListAsync();
            if (list == null) return null;

            //TODO:暂简单实现，待优化为排序后处理
            //var sortedList = list.OrderBy(t => t.GetEntityId(setModel.RefMemberId) ?? Guid.Empty); //TODO:check order
            //Dictionary<Guid, Entity> dic = new Dictionary<Guid, Entity>();
            var res = new EntityList(setModel);
            foreach (var obj in list /*sortedList*/)
            {
                //根据上级标识依次加入
                var parentId = obj.GetEntityId(refModel.FKMemberIds[0]);
                if (parentId == null)
                {
                    res.Add(obj);
                }
                else
                {
                    var parent = list.SingleOrDefault(t => (Guid)t.Id == (Guid)parentId);
                    if (parent != null)
                    {
                        parent.InitEntitySetForLoad(setModel); //先尝初始化EntitySet为已加载状态
                        parent.GetEntitySet(setMemberId).Add(obj);
                    }
                    else
                    {
                        res.Add(obj);
                    }
                    //if (dic.TryGetValue(parentId, out Entity parent))
                    //    parent.GetEntitySet(setMemberId).Add(obj);
                    //else
                    //res.Add(obj);
                }

                //dic.Add(obj.Id, obj);
            }
            return res;
        }
#endregion
    }
}

#endif