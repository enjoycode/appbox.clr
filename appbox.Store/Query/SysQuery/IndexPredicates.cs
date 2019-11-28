using System;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    public sealed class IndexPredicates
    {

        /// <summary>
        /// 用于分区表指定分区的谓词，按模型定义分区键顺序
        /// </summary>
        private KeyPredicate?[] predicates;

        internal bool IsOnlyEqual()
        {
            if (predicates == null) return true; //没有谓词视为相等性判断

            var res = true;
            for (int i = 0; i < predicates.Length; i++)
            {
                if (!predicates[i].HasValue) return res;
                if (predicates[i].Value.Type != KeyPredicateType.Equal) return false;
            }
            return res;
        }

        /// <summary>
        /// 添加索引谓词，由编译器确保Caller键正确性
        /// </summary>
        public void Where(KeyPredicate predicate, int index, int len)
        {
            if (predicates == null)
                predicates = new KeyPredicate?[len];
            predicates[index] = predicate;
        }

        internal unsafe int CalcKeySize(int* varSizes)
        {
            int keySize = KeyUtil.INDEXCF_PREFIX_SIZE;
            if (predicates != null)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (!predicates[i].HasValue) break; //没有指定谓词跳出

                    var m = predicates[i].Value.Value;
                    keySize += EntityStoreWriter.CalcMemberSize(ref m, varSizes + i, true);

                    if (predicates[i].Value.Type != KeyPredicateType.Equal) //非相等判断跳出
                        break;
                }
            }

            return keySize;
        }

        internal unsafe void WriteKeyRange(ulong groupId, EntityIndexModel model,
            byte* bk, byte* ek, int* varSizes)
        {
            EntityId.WriteRaftGroupId(bk, groupId);
            EntityId.WriteRaftGroupId(ek, groupId);
            bk[KeyUtil.INDEXCF_INDEXID_POS] = ek[KeyUtil.INDEXCF_INDEXID_POS] = model.IndexId;

            if (predicates == null || !predicates[0].HasValue) //short path for no predicate
            {
                ek[KeyUtil.INDEXCF_INDEXID_POS] = (byte)(model.IndexId + 1);
                return;
            }

            var bw = new EntityStoreWriter(bk, KeyUtil.INDEXCF_PREFIX_SIZE);
            var ew = new EntityStoreWriter(ek, KeyUtil.INDEXCF_PREFIX_SIZE);
            for (int i = 0; i < predicates.Length; i++)
            {
                if (!predicates[i].HasValue) break; //没有指定谓词跳出

                var m = predicates[i].Value.Value;

                switch (predicates[i].Value.Type)
                {
                    case KeyPredicateType.Equal:
                        {
                            //注意写入索引键的排序标记
                            bw.WriteMember(ref m, varSizes, true, model.Fields[i].OrderByDesc);
                            ew.WriteMember(ref m, varSizes, true, model.Fields[i].OrderByDesc);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
