#if FUTURE

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using appbox.Models;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 用于分区表及本地索引扫描时指定分区谓词
    /// </summary>
    public sealed class PartitionPredicates
    {

        /// <summary>
        /// 用于分区表指定分区的谓词，按模型定义分区键顺序
        /// </summary>
        private KeyPredicate?[] predicates;

        private bool IsAllEqual()
        {
            if (predicates == null) return false;

            for (int i = 0; i < predicates.Length; i++)
            {
                if (!predicates[i].HasValue) return false;
                if (predicates[i].Value.Type != KeyPredicateType.Equal) return false;
            }
            return true;
        }

        /// <summary>
        /// 添加指定分区谓词，由编译器确保Caller分区键正确性
        /// </summary>
        public void Where(KeyPredicate predicate, int pkIndex, int pkLen)
        {
            if (predicates == null)
                predicates = new KeyPredicate?[pkLen];
            predicates[pkIndex] = predicate;
        }

        internal async ValueTask<ulong[]> GetPartitions(byte appId, EntityModel model)
        {
            if (IsAllEqual()) //所有谓词都是相等判断，可以确认惟一分区走ReadIndexByGet
            {
                IntPtr keyPtr;
                int keySize;
                unsafe
                {
                    int* varSizes = stackalloc int[model.SysStoreOptions.PartitionKeys.Length];
                    keySize = CalcPartitionKeySize(predicates, model, varSizes);

                    var key = stackalloc byte[keySize];
                    WritePartitionKey(appId, model, key, varSizes);
                    keyPtr = new IntPtr(key);
                }

                var getRes = await StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID,
                    keyPtr, (uint)keySize, KeyUtil.PARTCF_INDEX);
                if (getRes == null) return null;
                ulong groupId;
                unsafe
                {
                    ulong* groupIdPtr = (ulong*)getRes.DataPtr.ToPointer();
                    groupId = *groupIdPtr;
                }
                return new ulong[] { groupId };
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 用于写入全部相等的分区Key
        /// </summary>
        private unsafe void WritePartitionKey(byte appId, EntityModel model, byte* key, int* varSizes)
        {
            var tableId = model.TableId;
            byte* tiPtr = (byte*)&tableId;

            key[0] = appId;
            key[1] = tiPtr[2];
            key[2] = tiPtr[1];
            key[3] = tiPtr[0];
            key[4] = KeyUtil.PARTCF_PART_TABLE_FLAG;

            var w = new EntityStoreWriter(key, 5);
            for (int i = 0; i < predicates.Length; i++)
            {
                Debug.Assert(predicates[i].Value.Type == KeyPredicateType.Equal);

                var m = predicates[i].Value.Value;
                var ruleArg = model.SysStoreOptions.PartitionKeys[i].RuleArgument;

                if (model.SysStoreOptions.PartitionKeys[i].Rule == PartitionKeyRule.Hash)
                {
                    int pkValue = EntityStoreWriter.GetHashOfPK(m.BoxedValue, ruleArg);
                    w.WriteUInt16((ushort)(m.Id | 4)); //不用写排序标记
                    w.WriteInt32(pkValue);
                }
                else if (model.SysStoreOptions.PartitionKeys[i].Rule == PartitionKeyRule.RangeOfDate)
                {
                    int pkValue = EntityStoreWriter.GetRangeOfPK(m.DateTimeValue, (DatePeriod)ruleArg);
                    ushort mid = m.Id;
                    if (model.SysStoreOptions.PartitionKeys[i].OrderByDesc)
                        mid |= 1 << IdUtil.MEMBERID_ORDER_OFFSET;
                    w.WriteUInt16((ushort)(mid | 4)); //写入排序标记
                    w.WriteInt32(pkValue);
                }
                else
                {
                    w.WriteMember(ref m, varSizes + i, true, model.SysStoreOptions.PartitionKeys[i].OrderByDesc);
                }
            }
        }

#region ====Static Methods====
        /// <summary>
        /// 根据分区键谓词计算扫描PartCF时KeySize
        /// </summary>
        private static unsafe int CalcPartitionKeySize(KeyPredicate?[] predicates, EntityModel model, int* varSizes)
        {
            int pkSize = 5;
            if (predicates != null)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (!predicates[i].HasValue) break; //没有指定谓词跳出

                    if (model.SysStoreOptions.PartitionKeys[i].Rule == PartitionKeyRule.None)
                    {
                        var m = predicates[i].Value.Value;
                        pkSize += EntityStoreWriter.CalcMemberSize(ref m, varSizes + i, true);
                    }
                    else
                    {
                        pkSize += 6; //暂统一2 + 4字节
                    }

                    if (predicates[i].Value.Type != KeyPredicateType.Equal) //非相等判断跳出
                        break;
                }
            }

            return pkSize;
        }

        private static unsafe void WritePartitionKeyRange(KeyPredicate?[] predicates,
            byte appId, EntityModel model,
            byte* bk, byte* ek, int* varSizes)
        {
            var tableId = model.TableId;
            byte* tiPtr = (byte*)&tableId;

            bk[0] = ek[0] = appId;
            bk[1] = ek[1] = tiPtr[2];
            bk[2] = ek[2] = tiPtr[1];
            bk[3] = ek[3] = tiPtr[0];
            bk[4] = ek[4] = KeyUtil.PARTCF_PART_TABLE_FLAG;

            if (predicates == null || !predicates[0].HasValue) //short path for no predicate
            {
                ek[4] = KeyUtil.PARTCF_PART_TABLE_FLAG + 1;
                return;
            }

            var bw = new EntityStoreWriter(bk, 5);
            var ew = new EntityStoreWriter(ek, 5);
            for (int i = 0; i < predicates.Length; i++)
            {
                if (!predicates[i].HasValue) break; //没有指定谓词跳出

                var m = predicates[i].Value.Value;
                var ruleArg = model.SysStoreOptions.PartitionKeys[i].RuleArgument;

                switch (predicates[i].Value.Type)
                {
                    case KeyPredicateType.Equal:
                        {
                            if (model.SysStoreOptions.PartitionKeys[i].Rule == PartitionKeyRule.Hash)
                            {
                                int pkValue = EntityStoreWriter.GetHashOfPK(m.BoxedValue, ruleArg);
                                bw.WriteUInt16((ushort)(m.Id | 4));
                                bw.WriteInt32(pkValue);
                                ew.WriteUInt16((ushort)(m.Id | 4));
                                ew.WriteInt32(pkValue);
                            }
                            else if (model.SysStoreOptions.PartitionKeys[i].Rule == PartitionKeyRule.RangeOfDate)
                            {

                            }
                            else
                            {

                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// 从存储PartCF加载分区集
        /// </summary>
        internal static async ValueTask<ulong[]> LoadPartitions(byte appId, EntityModel model, KeyPredicate?[] predicates)
        {
            IntPtr beginKeyPtr;
            IntPtr endKeyPtr;
            int keySize;
            unsafe
            {
                int* varSizes = stackalloc int[model.SysStoreOptions.PartitionKeys.Length];
                keySize = CalcPartitionKeySize(predicates, model, varSizes);

                var bk = stackalloc byte[keySize];
                var ek = stackalloc byte[keySize];
                WritePartitionKeyRange(predicates, appId, model, bk, ek, varSizes);

                beginKeyPtr = new IntPtr(bk);
                endKeyPtr = new IntPtr(ek);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = beginKeyPtr,
                BeginKeySize = new IntPtr(keySize),
                EndKeyPtr = endKeyPtr,
                EndKeySize = new IntPtr(keySize),
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = KeyUtil.PARTCF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (scanRes == null) return null;
            var raftGroupIds = new ulong[scanRes.Length];
            unsafe
            {
                int index = 0;
                scanRes.ForEachRow((kp, ks, vp, vs) =>
                {
                    //TODO:***进一步过滤未附加的Key的谓词
                    ulong* groupIdPtr = (ulong*)vp.ToPointer();
                    raftGroupIds[index] = *groupIdPtr;
                    index++;
                });
                scanRes.Dispose();
            }
            return raftGroupIds;
        }
#endregion
    }
}

#endif