#if FUTURE

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using appbox.Caching;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;
using appbox.Server;
using System.Collections.Generic;

namespace appbox.Store
{
    public static class EntityStore
    {
#region ====静态辅助方法====
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static unsafe bool MemorySame(void *src, int srcSize, void *dest, int destSize)
        //{
        //    if (srcSize != destSize) return false;

        //    var srcSpan = new ReadOnlySpan<byte>(src, srcSize);
        //    var destSpan = new ReadOnlySpan<byte>(dest, destSize);
        //    return srcSpan.SequenceEqual(destSpan);
        //}

        /// <summary>
        /// 比较两个Entity实例的成员值是否相同
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EntityMemberSame(Entity left, Entity right, ushort mid)
        {
            ref EntityMember lm = ref left.GetMember(mid);
            ref EntityMember rm = ref right.GetMember(mid);

            //Log.Debug($"比较成员{lm.HasValue} {rm.HasValue} {lm.GuidValue} {rm.GuidValue} {lm.ObjectValue} {rm.ObjectValue}");

            if (!lm.HasValue && !rm.HasValue) return true;
            if (lm.HasValue != rm.HasValue) return false;
            return lm.GuidValue == rm.GuidValue && Equals(lm.ObjectValue, rm.ObjectValue); //对象使用Equals比较，不要使用==
        }
#endregion

#region ====分区缓存相关操作====
        internal static async ValueTask<ulong> TryGetPartionByReadIndex(BytesKey partionKey)
        {
            var pkey = partionKey.CopyToManaged(); //必须在异步await前先Copy
            //查询分区并缓存
            var groupIdData = await StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID,
                                                             partionKey.unmanagedPtr, (uint)partionKey.unmanagedSize,
                                                             KeyUtil.PARTCF_INDEX);
            if (groupIdData == null)
                return 0;

            ulong groupId;
            unsafe
            {
                ulong* groupIdPtr = (ulong*)groupIdData.DataPtr.ToPointer();
                groupId = *groupIdPtr;
            }
            MetaCaches.PartitionCaches.TryAdd(pkey, groupId);
            return groupId;
        }

        /// <summary>
        /// 获取或创建非分区表的RaftGroupId
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="model">Model.</param>
        /// <param name="txnPtr">Null表示只Get不尝试新建.</param>
        internal static async ValueTask<ulong> GetOrCreateGlobalTablePartition(ApplicationModel app, EntityModel model, IntPtr txnPtr)
        {
            IntPtr partionInfoPtr;
            BytesKey partionKey;
            unsafe
            {
                int partionKeySize = 5;

                PartitionInfo* pi = stackalloc PartitionInfo[1];
                pi[0].Flags = model.SysStoreOptions.TableFlags;
                partionInfoPtr = new IntPtr(pi);

                byte* pkPtr = stackalloc byte[partionKeySize];
                pkPtr[0] = app.StoreId;
                var tableId = model.TableId;
                byte* tiPtr = (byte*)&tableId;
                pkPtr[1] = tiPtr[2];
                pkPtr[2] = tiPtr[1];
                pkPtr[3] = tiPtr[0];
                pkPtr[4] = KeyUtil.PARTCF_GLOBAL_TABLE_FLAG;

                pi[0].KeyPtr = new IntPtr(pkPtr);
                pi[0].KeySize = new IntPtr(partionKeySize);
                partionKey = new BytesKey(new IntPtr(pkPtr), partionKeySize);
            }

            //先查询缓存是否存在
            if (!MetaCaches.PartitionCaches.TryGet(partionKey, out ulong groupId))
            {
                if (txnPtr != IntPtr.Zero)
                {
                    var pkey = partionKey.CopyToManaged(); //必须先处理
                    //TODO:ReadIndex查询分区键是否存在，不存在再尝试创建分区
                    groupId = await StoreApi.Api.MetaGenPartitionAsync(txnPtr, partionInfoPtr);
                    MetaCaches.PartitionCaches.TryAdd(pkey, groupId);
                }
                else
                {
                    groupId = await TryGetPartionByReadIndex(partionKey);
                }
            }
            return groupId;
        }

        /// <summary>
        /// 获取或创建分区表的RaftGroupId
        /// </summary>
        /// <param name="app">App.</param>
        /// <param name="model">Model.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="txnPtr">Txn ptr.</param>
        internal static async ValueTask<ulong> GetOrCreatePartTablePartition(ApplicationModel app, EntityModel model, Entity entity, IntPtr txnPtr)
        {
            IntPtr partionInfoPtr;
            BytesKey partionKey;
            unsafe
            {
                int partionKeySize = 5;
                //根据是否分区设置设置typeFlag
                byte typeFlag = KeyUtil.PARTCF_PART_TABLE_FLAG;
                //计算分区键的大小
                int partitionKeysLen = model.SysStoreOptions.PartitionKeys.Length;
                int* varSizes = stackalloc int[partitionKeysLen];
                if (typeFlag == KeyUtil.PARTCF_PART_TABLE_FLAG)
                {
                    partionKeySize += EntityStoreWriter.CalcPartitionKeysSize(entity, model, varSizes);
                }

                PartitionInfo* pi = stackalloc PartitionInfo[1];
                pi[0].Flags = model.SysStoreOptions.TableFlags;
                partionInfoPtr = new IntPtr(pi);

                byte* pkPtr = stackalloc byte[partionKeySize];
                pkPtr[0] = app.StoreId;
                var tableId = model.TableId;
                byte* tiPtr = (byte*)&tableId;
                pkPtr[1] = tiPtr[2];
                pkPtr[2] = tiPtr[1];
                pkPtr[3] = tiPtr[0];
                pkPtr[4] = typeFlag;
                //写入分区键值
                EntityStoreWriter.WritePartitionKeys(entity, model, pkPtr, varSizes);

                pi[0].KeyPtr = new IntPtr(pkPtr);
                pi[0].KeySize = new IntPtr(partionKeySize);
                partionKey = new BytesKey(new IntPtr(pkPtr), partionKeySize);
            }

            if (!MetaCaches.PartitionCaches.TryGet(partionKey, out ulong groupId))
            {
                var pkey = partionKey.CopyToManaged(); //必须先处理
                //TODO:ReadIndex查询分区键是否存在，不存在再尝试创建分区
                groupId = await StoreApi.Api.MetaGenPartitionAsync(txnPtr, partionInfoPtr);
                MetaCaches.PartitionCaches.TryAdd(pkey, groupId);
            }

            return groupId;
        }

        /// <summary>
        /// Gets the or create index partion async.
        /// </summary>
        /// <returns>0=不存在</returns>
        /// <param name="txnPtr">Null表示只Get不尝试新建</param>
        internal static async ValueTask<ulong> GetOrCreateIndexPartionAsync(EntityIndexModel indexModel, IntPtr txnPtr)
        {
            var app = await RuntimeContext.Current.GetApplicationModelAsync(indexModel.Owner.AppId);
            byte appId = app.StoreId;
            uint tableId = indexModel.Owner.TableId;

            //TODO:根据类型生成不同的PartionKey,暂只处理全局不自动Split的索引
            IntPtr partionInfoPtr;
            BytesKey partionKey;
            unsafe
            {
                int partionKeySize = 0;
                byte typeFlag = KeyUtil.PARTCF_GLOBAL_INDEX_FLAG; //TODO:根据是否分区设置

                PartitionInfo* pi = stackalloc PartitionInfo[1];
                pi[0].Flags = indexModel.Owner.SysStoreOptions.IndexFlags;
                partionInfoPtr = new IntPtr(pi);

                byte* pkPtr = stackalloc byte[6 + partionKeySize];
                pkPtr[0] = appId;
                byte* tiPtr = (byte*)&tableId;
                pkPtr[1] = tiPtr[2];
                pkPtr[2] = tiPtr[1];
                pkPtr[3] = tiPtr[0];
                pkPtr[4] = typeFlag;
                pkPtr[5] = indexModel.IndexId;
                pi[0].KeyPtr = new IntPtr(pkPtr);
                pi[0].KeySize = new IntPtr(6 + partionKeySize);

                partionKey = new BytesKey(new IntPtr(pkPtr), 6 + partionKeySize);
            }

            //先查询缓存是否存在, TODO:全局自动Split索引处理
            if (!MetaCaches.PartitionCaches.TryGet(partionKey, out ulong groupId))
            {
                //Log.Debug($"索引分区缓存不存在: {partionKey.DebugString}, {AppDomain.CurrentDomain.FriendlyName}");
                //TODO: ReadIndex查询分区键是否存在

                if (txnPtr != IntPtr.Zero)
                {
                    var pkey = partionKey.CopyToManaged(); //必须先处理
                    groupId = await StoreApi.Api.MetaGenPartitionAsync(txnPtr, partionInfoPtr);
                    MetaCaches.PartitionCaches.TryAdd(pkey, groupId);
                }
                else
                {
                    groupId = await TryGetPartionByReadIndex(partionKey);
                }
            }

            return groupId;
        }
#endregion

#region ====数据及索引相关操作====
        //TODO:*** Insert/Update/Delete本地索引及数据通过BatchCommand优化，减少RPC次数

        internal static async ValueTask InsertEntityAsync(Entity entity, Transaction txn)
        {
            //TODO:考虑自动新建事务, 分区已存在且模型没有索引没有关系则可以不需要事务
            if (txn == null)
                throw new Exception("Must enlist transaction");
            //TODO:判断模型运行时及持久化状态
            var model = entity.Model; //直接指向肯定存在，不需要RuntimeContext.Current.GetEntityModel
            //暂不允许没有成员的插入操作
            if (model.Members.Count == 0)
                throw new NotSupportedException($"Entity[{model.Name}] has no member");

            var app = await RuntimeContext.Current.GetApplicationModelAsync(model.AppId);
            ulong groupId;
            if (model.SysStoreOptions.HasPartitionKeys)
                groupId = await GetOrCreatePartTablePartition(app, model, entity, txn.Handle);
            else
                groupId = await GetOrCreateGlobalTablePartition(app, model, txn.Handle);
            if (groupId == 0)
                throw new Exception("Can't get or create partition.");

            //设置Entity.Id.RaftGroupId
            entity.Id.InitRaftGroupId(groupId);

            //判断有无强制外键引用，有则先处理
            var refs = model.GetEntityRefsWithFKConstraint();
            if (refs != null)
            {
                for (int i = 0; i < refs.Count; i++)
                {
                    await txn.AddEntityRefAsync(refs[i], app, entity, 1);
                }
            }

            //插入索引，注意变更后可能已添加或删除了索引会报错
            if (model.SysStoreOptions.HasIndexes)
            {
                //TODO:并发插入索引，暂顺序处理，可考虑先处理惟一索引
                //TODO:暂只处理分区本地索引
                for (int i = 0; i < model.SysStoreOptions.Indexes.Count; i++)
                {
                    if (model.SysStoreOptions.Indexes[i].Global)
                        throw ExceptionHelper.NotImplemented();
                    await InsertLocalIndexAsync(entity, model.SysStoreOptions.Indexes[i], txn);
                }
            }

            //插入数据
            IntPtr dataPtr = EntityStoreWriter.WriteEntity(entity, out _);
            IntPtr keyPtr;
            unsafe
            {
                byte* pkPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
                KeyUtil.WriteEntityKey(pkPtr, entity.Id);
                keyPtr = new IntPtr(pkPtr);
            }

            //call native api
            var req = new ClrInsertRequire()
            {
                RaftGroupId = groupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.ENTITY_KEY_SIZE),
                DataPtr = dataPtr,
                SchemaVersion = model.SysStoreOptions.SchemaVersion,
                OverrideIfExists = false,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe
            {
                reqPtr = new IntPtr(&req);
                if (refs != null)
                {
                    var refsPtr = stackalloc ushort[refs.Count];
                    for (int i = 0; i < refs.Count; i++)
                    {
                        refsPtr[i] = refs[i].FKMemberIds[0]; //model.GetMember(refs[i].Name + "Id", true).MemberId;
                    }
                    req.RefsPtr = new IntPtr(refsPtr);
                    req.RefsSize = new IntPtr(refs.Count * 2);
                }
            }
            await StoreApi.Api.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask UpdateEntityAsync(Entity entity, Transaction txn)
        {
            if (entity == null || txn == null)
                throw new ArgumentNullException();

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(entity.ModelId);
            var app = await RuntimeContext.Current.GetApplicationModelAsync(model.AppId);
            //先获取强制外键引用
            var refs = model.GetEntityRefsWithFKConstraint();

            //更新数据 //TODO:暂使用全部更新的方式实现，待改为只更新变更过的成员
            IntPtr dataPtr = EntityStoreWriter.WriteEntity(entity, out _);
            IntPtr keyPtr;
            unsafe
            {
                byte* pkPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
                KeyUtil.WriteEntityKey(pkPtr, entity.Id);
                keyPtr = new IntPtr(pkPtr);
            }
            var req = new ClrUpdateRequire
            {
                RaftGroupId = entity.Id.RaftGroupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.ENTITY_KEY_SIZE),
                DataPtr = dataPtr,
                SchemaVersion = model.SysStoreOptions.SchemaVersion,
                DataCF = -1,
                Merge = false,
                ReturnExists = model.SysStoreOptions.HasIndexes || refs != null,
            };
            IntPtr reqPtr;
            unsafe
            {
                reqPtr = new IntPtr(&req);
                if (refs != null)
                {
                    var refsPtr = stackalloc ushort[refs.Count];
                    for (int i = 0; i < refs.Count; i++)
                    {
                        refsPtr[i] = model.GetMember(refs[i].Name + "Id", true).MemberId;
                    }
                    req.RefsPtr = new IntPtr(refsPtr);
                    req.RefsSize = new IntPtr(refs.Count * 2);
                }
            }
            var res = await StoreApi.Api.ExecKVUpdateAsync(txn.Handle, reqPtr);

            //根据返回值处理变更的索引及外键引用
            if (res != null && (model.SysStoreOptions.HasIndexes || refs != null))
            {
                var storedEntity = new Entity(model, entity.Id);
                EntityStoreReader.ReadEntityFields(model, storedEntity, res.DataPtr, (int)res.Size);

                if (model.SysStoreOptions.HasIndexes)
                {
                    //处理变更的索引
                    for (int j = 0; j < model.SysStoreOptions.Indexes.Count; j++)
                    {
                        var indexModel = model.SysStoreOptions.Indexes[j];
                        if (indexModel.Global)
                            throw ExceptionHelper.NotImplemented();

                        bool indexKeyChanged = false;
                        for (int i = 0; i < indexModel.Fields.Length; i++)
                        {
                            if (!EntityMemberSame(storedEntity, entity, indexModel.Fields[i].MemberId))
                            {
                                indexKeyChanged = true; break;
                            }
                        }
                        if (indexKeyChanged)
                        {
                            //删除旧的索引再添加新的索引
                            await DeleteLocalIndexAsync(storedEntity, indexModel, txn);
                            await InsertLocalIndexAsync(entity, indexModel, txn);
                        }
                        else
                        {
                            //比较Convering数据部分有无改变，改变则更新索引值
                            if (indexModel.HasStoringFields)
                            {
                                bool indexValueChanged = false;
                                for (int k = 0; k < indexModel.StoringFields.Length; k++)
                                {
                                    if (!EntityMemberSame(storedEntity, entity, indexModel.StoringFields[k]))
                                    {
                                        indexValueChanged = true; break;
                                    }
                                }
                                if (indexValueChanged)
                                {
                                    await UpdateLocalIndexAsync(entity, indexModel, txn);
                                }
                            }
                        }
                    }
                }

                if (refs != null)
                {
                    for (int i = 0; i < refs.Count; i++)
                    {
                        var oldField = storedEntity.GetEntityId(refs[i].FKMemberIds[0]);
                        var newField = entity.GetEntityId(refs[i].FKMemberIds[0]);
                        if (oldField != null)
                        {
                            if (newField != null)
                            {
                                if (newField != oldField)
                                {
                                    await txn.AddEntityRefAsync(refs[i], app, storedEntity, -1);
                                    await txn.AddEntityRefAsync(refs[i], app, entity, 1);
                                }
                            }
                            else
                            {
                                await txn.AddEntityRefAsync(refs[i], app, storedEntity, -1);
                            }
                        }
                        else
                        {
                            if (newField != null)
                            {
                                await txn.AddEntityRefAsync(refs[i], app, entity, 1);
                            }
                        }
                    }
                }
            }
        }

        internal static async ValueTask DeleteEntityAsync(EntityModel model, EntityId id, Transaction txn)
        {
            if (id == null || txn == null || model == null)
                throw new ArgumentNullException();

            //注意删除前先处理本事务挂起的外键引用，以防止同一事务删除引用后再删除引用目标失败(eg:同一事务删除订单明细，删除引用的订单)
            await txn.ExecPendingRefs();

            var app = await RuntimeContext.Current.GetApplicationModelAsync(model.AppId);
            //先获取强制外键引用
            var refs = model.GetEntityRefsWithFKConstraint();

            //删除数据
            IntPtr keyPtr;
            unsafe
            {
                byte* pkPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
                KeyUtil.WriteEntityKey(pkPtr, id);
                keyPtr = new IntPtr(pkPtr);
            }

            //TODO:参数控制返回索引字段的值，用于生成索引键，暂返回全部字段
            var req = new ClrDeleteRequire
            {
                RaftGroupId = id.RaftGroupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.ENTITY_KEY_SIZE),
                SchemaVersion = model.SysStoreOptions.SchemaVersion,
                ReturnExists = model.SysStoreOptions.HasIndexes || refs != null,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe
            {
                reqPtr = new IntPtr(&req);
                if (refs != null)
                {
                    var refsPtr = stackalloc ushort[refs.Count];
                    for (int i = 0; i < refs.Count; i++)
                    {
                        refsPtr[i] = refs[i].FKMemberIds[0];//model.GetMember(refs[i].Name + "Id", true).MemberId;
                    }
                    req.RefsPtr = new IntPtr(refsPtr);
                    req.RefsSize = new IntPtr(refs.Count * 2);
                }
            }
            var res = await StoreApi.Api.ExecKVDeleteAsync(txn.Handle, reqPtr);

            //删除索引并扣减引用计数
            if (res != null && (model.SysStoreOptions.HasIndexes || refs != null))
            {
                var storedEntity = new Entity(model, id);
                EntityStoreReader.ReadEntityFields(model, storedEntity, res.DataPtr, (int)res.Size);

                if (model.SysStoreOptions.HasIndexes)
                {
                    for (int i = 0; i < model.SysStoreOptions.Indexes.Count; i++)
                    {
                        if (model.SysStoreOptions.Indexes[i].Global)
                            throw ExceptionHelper.NotImplemented();
                        await DeleteLocalIndexAsync(storedEntity, model.SysStoreOptions.Indexes[i], txn);
                    }
                }

                if (refs != null)
                {
                    for (int i = 0; i < refs.Count; i++)
                    {
                        await txn.AddEntityRefAsync(refs[i], app, storedEntity, -1);
                    }
                }
            }
        }

        /// <summary>
        /// 插入分区本地索引
        /// </summary>
        private static async ValueTask InsertLocalIndexAsync(Entity entity,
            EntityIndexModel indexModel, Transaction txn)
        {
            if (entity.Id.RaftGroupId == 0) throw new ArgumentException();
            if (txn == null) throw new ArgumentNullException();

            IntPtr keyPtr;
            //非惟一索引加上指向的EntityId's Part2
            int keySize = indexModel.Unique ? KeyUtil.INDEXCF_PREFIX_SIZE : KeyUtil.INDEXCF_PREFIX_SIZE + 10;
            unsafe
            {
                int* varSizes = stackalloc int[indexModel.Fields.Length]; //主要用于记录String utf8数据长度,避免重复计算
                for (int i = 0; i < indexModel.Fields.Length; i++)
                {
                    var fieldSize = EntityStoreWriter.CalcMemberSize(ref entity.GetMember(indexModel.Fields[i].MemberId),
                        varSizes + i, !indexModel.Unique);
                    if (indexModel.Unique && fieldSize == 0) //注意:暂惟一索引且不具备值则退出
                        return;
                    keySize += fieldSize;
                }

                byte* pkPtr = stackalloc byte[keySize];
                var writer = new EntityStoreWriter(pkPtr, 0);
                writer.WriteIndexKey(entity, indexModel, varSizes);
                keyPtr = new IntPtr(pkPtr);
            }

            IntPtr dataPtr = EntityStoreWriter.WriteIndexData(entity, indexModel, out _);
            var req = new ClrInsertRequire()
            {
                RaftGroupId = entity.Id.RaftGroupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                DataPtr = dataPtr,
                SchemaVersion = indexModel.Owner.SysStoreOptions.SchemaVersion,
                OverrideIfExists = false,
                DataCF = KeyUtil.INDEXCF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await StoreApi.Api.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        /// <summary>
        /// 插入全局索引
        /// </summary>
        //private static async ValueTask InsertGlobalIndexAsync(ApplicationModel app, 
        //    Entity entity, EntityIndexModel indexModel, Transaction txn)
        //{
        //    //TODO:全局AutoSplit索引需要先生成IndexCF Key
        //    //找到索引所在RaftGroup
        //    //先查询是否存在, TODO:全局自动Split索引处理
        //    ulong groupId = await GetOrCreateIndexPartionAsync(indexModel, txn.Handle);
        //    if (groupId == 0)
        //        throw new Exception("Can't GetOrCreate index partion");
        //}

        /// <summary>
        /// 仅用于更新Convering Index的值部分
        /// </summary>
        private static async ValueTask UpdateLocalIndexAsync(Entity entity,
            EntityIndexModel indexModel, Transaction txn)
        {
            if (entity.Id.RaftGroupId == 0) throw new ArgumentException();
            if (txn == null) throw new ArgumentNullException();

            IntPtr keyPtr;
            //非惟一索引加上指向的EntityId's Part2
            int keySize = indexModel.Unique ? KeyUtil.INDEXCF_PREFIX_SIZE : KeyUtil.INDEXCF_PREFIX_SIZE + 10;
            unsafe
            {
                int* varSizes = stackalloc int[indexModel.Fields.Length]; //主要用于记录String utf8数据长度,避免重复计算
                for (int i = 0; i < indexModel.Fields.Length; i++)
                {
                    var fieldSize = EntityStoreWriter.CalcMemberSize(ref entity.GetMember(indexModel.Fields[i].MemberId),
                        varSizes + i, !indexModel.Unique);
                    if (indexModel.Unique && fieldSize == 0) //注意:暂惟一索引且不具备值则退出
                        return;
                    keySize += fieldSize;
                }

                byte* pkPtr = stackalloc byte[keySize];
                var writer = new EntityStoreWriter(pkPtr, 0);
                writer.WriteIndexKey(entity, indexModel, varSizes);
                keyPtr = new IntPtr(pkPtr);
            }

            IntPtr dataPtr = EntityStoreWriter.WriteIndexData(entity, indexModel, out int dataSize);
            var req = new ClrUpdateRequire
            {
                RaftGroupId = entity.Id.RaftGroupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                DataPtr = dataPtr,
                SchemaVersion = indexModel.Owner.SysStoreOptions.SchemaVersion,
                DataCF = KeyUtil.INDEXCF_INDEX,
                Merge = false,
                ReturnExists = false
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await StoreApi.Api.ExecKVUpdateAsync(txn.Handle, reqPtr);
        }

        /// <summary>
        /// 删除分区本地索引
        /// </summary>
        private static async ValueTask DeleteLocalIndexAsync(Entity entity,
            EntityIndexModel indexModel, Transaction txn)
        {
            if (indexModel.Global) throw new NotSupportedException();
            if (entity.Id.RaftGroupId == 0) throw new ArgumentException();
            if (txn == null) throw new ArgumentNullException();

            IntPtr keyPtr;
            //非惟一索引加上指向的EntityId's Part2
            int keySize = indexModel.Unique ? KeyUtil.INDEXCF_PREFIX_SIZE : KeyUtil.INDEXCF_PREFIX_SIZE + 10;
            unsafe
            {
                int* varSizes = stackalloc int[indexModel.Fields.Length]; //主要用于记录String utf8数据长度,避免重复计算
                for (int i = 0; i < indexModel.Fields.Length; i++)
                {
                    var fieldSize = EntityStoreWriter.CalcMemberSize(ref entity.GetMember(indexModel.Fields[i].MemberId),
                        varSizes + i, !indexModel.Unique);
                    if (indexModel.Unique && fieldSize == 0) //注意:暂惟一索引且不具备值则退出
                        return;
                    keySize += fieldSize;
                }

                byte* pkPtr = stackalloc byte[keySize];
                var writer = new EntityStoreWriter(pkPtr, 0);
                writer.WriteIndexKey(entity, indexModel, varSizes);
                keyPtr = new IntPtr(pkPtr);
            }

            var req = new ClrDeleteRequire
            {
                RaftGroupId = entity.Id.RaftGroupId,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                SchemaVersion = indexModel.Owner.SysStoreOptions.SchemaVersion,
                ReturnExists = false,
                DataCF = KeyUtil.INDEXCF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await StoreApi.Api.ExecKVDeleteAsync(txn.Handle, reqPtr);
        }

        //private static async ValueTask DeleteIndexAsync(ApplicationModel app, Entity entity, EntityIndexModel indexModel, Transaction txn)
        //{
        //    ulong groupId = await GetOrCreateIndexPartionAsync(indexModel, IntPtr.Zero); //不需要创建
        //    if (groupId == 0)
        //        throw new Exception("Can't get index partion");
        //}
#endregion

#region ====由服务调用的简化方法====
        public static async ValueTask SaveAsync(Entity entity)
        {
            using (var txn = await Transaction.BeginAsync())
            {
                await SaveAsync(entity, txn);
                await txn.CommitAsync();
            }
        }

        public static async ValueTask SaveAsync(Entity entity, Transaction txn)
        {
            if (txn == null)
                throw new ArgumentNullException(nameof(txn));

            switch (entity.PersistentState)
            {
                case PersistentState.Detached:
                    await InsertEntityAsync(entity, txn);
                    break;
                case PersistentState.Modified:
                case PersistentState.Unchanged: //TODO: remove this, test only
                    await UpdateEntityAsync(entity, txn);
                    break;
                default:
                    throw ExceptionHelper.NotImplemented();
            }
        }

        public static ValueTask DeleteAsync(Entity entity)
        {
            return DeleteAsync(entity.ModelId, entity.Id);
        }

        public static ValueTask DeleteAsync(Entity entity, Transaction txn)
        {
            return DeleteAsync(entity.ModelId, entity.Id, txn);
        }

        public static async ValueTask DeleteAsync(ulong modelId, EntityId id)
        {
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            using (var txn = await Transaction.BeginAsync())
            {
                await DeleteEntityAsync(model, id, txn);
                await txn.CommitAsync();
            }
        }

        public static async ValueTask DeleteAsync(ulong modelId, EntityId id, Transaction txn)
        {
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            await DeleteEntityAsync(model, id, txn);
        }

        /// <summary>
        /// 从存储根据Id加载单个Entity实例
        /// </summary>
        public static async ValueTask<Entity> LoadAsync(ulong modelId, Guid id)
        {
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);

            IntPtr keyPtr;
            unsafe
            {
                byte* pkPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
                KeyUtil.WriteEntityKey(pkPtr, id);
                keyPtr = new IntPtr(pkPtr);
            }

            var groupId = EntityId.GetRaftGroupId(id);
            var res = await StoreApi.Api.ReadIndexByGetAsync(groupId, keyPtr, KeyUtil.ENTITY_KEY_SIZE);
            if (res == null)
                return null;

            var obj = new Entity(model, id);
            EntityStoreReader.ReadEntityFields(model, obj, res.DataPtr, (int)res.Size);
            res.Dispose();
            return obj;
        }

        public static async ValueTask<TreeNodePath> LoadTreeNodePathAsync(ulong modelId, Guid id, ushort parentMemberId, ushort textMemberId)
        {
            //TODO:*****暂简单实现(需要快照事务读，另外考虑存储引擎实现一次加载)
            var leaf = await LoadAsync(modelId, id);
            if (leaf == null) return null;

            var list = new List<TreeNodeInfo>();
            var refModel = (EntityRefModel)leaf.Model.GetMember(parentMemberId, true);
            await LoopLoadTreeNode(leaf, list, refModel, textMemberId);
            return new TreeNodePath(list);
        }

        private static async ValueTask LoopLoadTreeNode(Entity node, List<TreeNodeInfo> list, EntityRefModel refModel, ushort textMemberId)
        {
            list.Add(new TreeNodeInfo { ID = node.Id, Text = node.GetString(textMemberId) });

            var parentId = node.GetEntityId(refModel.FKMemberIds[0]);
            if (parentId == null) return;

            var parent = await LoadAsync(node.ModelId, parentId);
            if (parent != null)
            {
                await LoopLoadTreeNode(parent, list, refModel, textMemberId);
            }
        }

        /// <summary>
        /// 利用引用索引加载指定实体的EntitySet集合，如加载订单.订单明细
        /// </summary>
        public static async ValueTask<EntityList> LoadEntitySetAsync(ulong modelId, Guid id, ushort setMemberId)
        {
            //TODO: 待支持从已知分区查询
            //TODO:*****开启快照读事务

            //注意：目前只有外键约束的EntitySet
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            var setModel = (EntitySetModel)model.GetMember(setMemberId, true);

            var targetModel = await RuntimeContext.Current.GetModelAsync<EntityModel>(setModel.RefModelId);
            var targetApp = await RuntimeContext.Current.GetApplicationModelAsync(targetModel.AppId);
            var targetRefModel = (EntityRefModel)targetModel.GetMember(setModel.RefMemberId, true);
            uint fromTableId = KeyUtil.EncodeTableId(targetApp.StoreId, targetModel.TableId);

            //1.从RefIndexCF's RefFrom查询引用的所有分区
            IntPtr fromKeyPtr;
            int fromKeySize = KeyUtil.REFINDEXCF_REFFROM_KEYSIZE - 8; //不包含FromRaftGroupId
            unsafe
            {
                byte* bk = stackalloc byte[fromKeySize];
                KeyUtil.WriteRefFromKeyPrefix(bk, id, fromTableId);
                fromKeyPtr = new IntPtr(bk);
            }
            var req = new ClrScanRequire
            {
                RaftGroupId = EntityId.GetRaftGroupId(id),
                BeginKeyPtr = fromKeyPtr,
                BeginKeySize = new IntPtr(fromKeySize),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = KeyUtil.REFINDEXCF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var fromGroupsRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (fromGroupsRes == null || fromGroupsRes.Length == 0) return null;

            var fromGroups = new ulong[fromGroupsRes.Length];
            var groupIndex = 0;
            fromGroupsRes.ForEachRow((kp, ks, vp, vs) =>
            {
                fromGroups[groupIndex++] = KeyUtil.GetRaftGroupIdFromRefFromKey(kp, ks); //TODO:考虑底层只返回RaftGroupIds
            });
            fromGroupsRes.Dispose();

            //2.从RefIndexCF's RefTo引用的分区查询出目标
            var list = new EntityList();
            for (int i = 0; i < fromGroups.Length; i++)
            {
                IntPtr toKeyPtr;
                int toKeySize = KeyUtil.REFINDEXCF_REFTO_KEYSIZE - 10; //不包含SelfEntityId's Part2
                unsafe
                {
                    byte* bk = stackalloc byte[toKeySize];
                    KeyUtil.WriteRefToKeyPrefix(bk, id, targetRefModel.FKMemberIds[0], fromGroups[i]);
                    toKeyPtr = new IntPtr(bk);
                }
                var req2 = new ClrScanRequire
                {
                    RaftGroupId = fromGroups[i],
                    BeginKeyPtr = toKeyPtr,
                    BeginKeySize = new IntPtr(toKeySize),
                    EndKeyPtr = IntPtr.Zero,
                    EndKeySize = IntPtr.Zero,
                    FilterPtr = IntPtr.Zero,
                    Skip = 0,
                    Take = uint.MaxValue,
                    DataCF = KeyUtil.REFINDEXCF_INDEX
                };
                IntPtr reqPtr2;
                unsafe { reqPtr2 = new IntPtr(&req2); }
                var entities = await StoreApi.Api.ReadIndexByScanAsync(reqPtr2);
                if (entities != null && entities.Length > 0)
                {
                    //注意：存储引擎扫描RefTo时已经返回指向的Entity的数据
                    entities.ForEachRow((kp, ks, vp, vs) =>
                    {
                        var entity = EntityStoreReader.ReadEntity(targetModel, kp, ks, vp, vs);
                        list.Add(entity);
                    });
                }
            }

            return list;
        }
#endregion
    }
}

#endif