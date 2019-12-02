using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;
using appbox.Server;

namespace appbox.Store
{
    //TODO:外键引用处理考虑在存储层实现，因为可能需要实现跨进程序列化传输事务

    public sealed class Transaction : IDisposable, ITransaction
    {
        internal IntPtr Handle { get; }
        private int status; //TODO:remove it
        private List<RefFromItem> refs; //本事务缓存的需要处理的外键引用计数

        Transaction(IntPtr nativeHandle)
        {
            Handle = nativeHandle;
        }

        public static async ValueTask<Transaction> BeginAsync()
        {
            var nativeHandle = await StoreApi.Api.BeginTransactionAsync(true);
            return new Transaction(nativeHandle);
        }

        public async ValueTask CommitAsync()
        {
            if (Interlocked.CompareExchange(ref status, 1, 0) != 0)
                throw new Exception("Transaction has committed or rollback");

            await ExecPendingRefs(); //递交前先处理挂起的外键引用
            await StoreApi.Api.CommitTransactionAsync(Handle);
        }

        public void Rollback()
        {
            if (Interlocked.CompareExchange(ref status, 2, 0) != 0)
                return;

            StoreApi.Api.RollbackTransaction(Handle, false);
        }

        /// <summary>
        /// 仅用于测试，模拟事务所在的节点crash
        /// </summary>
        public void Abort()
        {
            if (Interlocked.CompareExchange(ref status, 3, 0) != 0)
                return;

            StoreApi.Api.RollbackTransaction(Handle, true);
        }

        #region ====外键引用相关====
        /// <summary>
        /// 增减外键引用计数值
        /// </summary>
        internal async ValueTask AddEntityRefAsync(EntityRefModel entityRef,
            ApplicationModel fromApp, Entity fromEntity, int diff)
        {
            Debug.Assert(diff != 0);
            Debug.Assert(fromEntity.Id.RaftGroupId != 0);

            var targetId = fromEntity.GetEntityId(entityRef.FKMemberIds[0]);
            if (targetId == null || targetId.IsEmpty) return;
            ulong targetModelId = entityRef.IsAggregationRef ? fromEntity.GetUInt64(entityRef.TypeMemberId) : entityRef.RefModelIds[0];
            var targetModel = await RuntimeContext.Current.GetModelAsync<EntityModel>(targetModelId);
            var targetAppId = IdUtil.GetAppIdFromModelId(targetModelId);
            var targetApp = await RuntimeContext.Current.GetApplicationModelAsync(targetAppId);
            //注意编码
            uint fromTableId = KeyUtil.EncodeTableId(fromApp.StoreId, entityRef.Owner.TableId);
           
            var item = new RefFromItem()
            {
                TargetEntityId = targetId,
                FromTableId = fromTableId,
                FromRaftGroupId = fromEntity.Id.RaftGroupId
            };

            lock (this)
            {
                if (refs == null)
                {
                    item.Diff = diff;
                    refs = new List<RefFromItem> { item };
                }
                else
                {
                    for (int i = 0; i < refs.Count; i++)
                    {
                        if (refs[i].TargetEntityId == targetId && refs[i].FromRaftGroupId == fromEntity.Id.RaftGroupId)
                        {
                            item.Diff = refs[i].Diff + diff;
                            refs[i] = item;
                            return;
                        }
                    }

                    //未找到
                    item.Diff = diff;
                    refs.Add(item);
                }
            }
        }

        /// <summary>
        /// 处理缓存的外键引用，执行后清空
        /// </summary>
        internal async ValueTask ExecPendingRefs()
        {
            if (refs == null) return;

            for (int i = 0; i < refs.Count; i++) //TODO:并行处理
            {
                Debug.Assert(refs[i].TargetEntityId.RaftGroupId != 0);
                if (refs[i].Diff == 0) continue;

                var req = new ClrAddRefRequire
                {
                    TargetRaftGroupId = refs[i].TargetEntityId.RaftGroupId,
                    FromRaftGroupId = refs[i].FromRaftGroupId,
                    FromTableId = refs[i].FromTableId,
                    Diff = refs[i].Diff
                };
                IntPtr reqPtr;
                unsafe
                {
                    byte* pkPtr = stackalloc byte[KeyUtil.ENTITY_KEY_SIZE];
                    KeyUtil.WriteEntityKey(pkPtr, refs[i].TargetEntityId);
                    req.KeyPtr = new IntPtr(pkPtr);
                    req.KeySize = new IntPtr(KeyUtil.ENTITY_KEY_SIZE);

                    reqPtr = new IntPtr(&req);
                }
                await StoreApi.Api.ExecKVAddRefAsync(Handle, reqPtr);
            }

            //别忘了清空
            refs.Clear();
        }

        private struct RefFromItem
        {
            internal EntityId TargetEntityId;
            internal ulong FromRaftGroupId;
            internal uint FromTableId; //注意已包含AppId且按大字节序编码
            internal int Diff;
        }
        #endregion

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Rollback();
                disposedValue = true;
            }
        }

        ~Transaction()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
