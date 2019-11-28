using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Store
{
    public sealed class IndexGet
    {

        private readonly EntityIndexModel _indexModel;
        private readonly KeyPredicate[] _predicates;

        #region ====Ctor====
        public IndexGet(ulong entityModelId, byte indexId)
        {
            var model = RuntimeContext.Current.GetModelAsync<EntityModel>(entityModelId).Result;
            if (model == null)
                throw new Exception($"Can't get entity model of :{entityModelId}");
            if (model.SysStoreOptions == null || !model.SysStoreOptions.HasIndexes)
                throw new Exception($"Can't get index model of:{indexId}");
            EntityIndexModel indexModel = null;
            for (int i = 0; i < model.SysStoreOptions.Indexes.Count; i++)
            {
                if (model.SysStoreOptions.Indexes[i].IndexId == indexId)
                {
                    indexModel = model.SysStoreOptions.Indexes[i];
                    break;
                }
            }
            if (indexModel == null)
                throw new Exception($"Can't get index model of:{indexId}");
            if (!indexModel.Unique)
                throw new Exception("Only unique index can use IndexGet");

            _indexModel = indexModel;
            _predicates = new KeyPredicate[_indexModel.Fields.Length];
        }

        public IndexGet(EntityIndexModel indexModel)
        {
            if (indexModel == null)
                throw new ArgumentNullException(nameof(indexModel));
            if (!indexModel.Unique)
                throw new Exception("Only unique index can use IndexGet");

            _indexModel = indexModel;
            _predicates = new KeyPredicate[_indexModel.Fields.Length];
        }
        #endregion

        public IndexGet Where(KeyPredicate cond)
        {
            for (int i = 0; i < _indexModel.Fields.Length; i++)
            {
                if (_indexModel.Fields[i].MemberId == cond.Value.Id)
                {
                    _predicates[i] = cond;
                    return this;
                }
            }
            throw new Exception($"Field[{cond.Value.Id}] not exists in index[{_indexModel.Name}]");
        }

        private void ValidatePredicates()
        {
            for (int i = 0; i < _predicates.Length; i++)
            {
                if (_predicates[i].Value.Id == 0 || _predicates[i].Type != KeyPredicateType.Equal)
                    throw new Exception("Key predicates error");
                //TODO:验证Value类型
            }
        }

        public async ValueTask<IndexRow> ToIndexRowAsync()
        {
            ValidatePredicates();

            var app = await RuntimeContext.Current.GetApplicationModelAsync(_indexModel.Owner.AppId);

            //Console.WriteLine($"IndexGet.ToIndexRowAsync: {StringHelper.ToHexString(keyPtr, keySize)}");
            //TODO:*****暂只支持非分区表索引
            if (_indexModel.Global || _indexModel.Owner.SysStoreOptions.HasPartitionKeys)
                throw ExceptionHelper.NotImplemented();

            //先获取目标分区
            ulong groupId = await EntityStore.GetOrCreateGlobalTablePartition(app, _indexModel.Owner, IntPtr.Zero);
            if (groupId == 0)
            {
                Log.Warn("Can't find index partition");
                return IndexRow.Empty;
            }

            //生成Key
            IntPtr keyPtr;
            int keySize = KeyUtil.INDEXCF_PREFIX_SIZE;
            unsafe
            {
                int* varSizes = stackalloc int[_indexModel.Fields.Length]; //主要用于记录String utf8数据长度,避免重复计算
                for (int i = 0; i < _indexModel.Fields.Length; i++)
                {
                    keySize += EntityStoreWriter.CalcMemberSize(ref _predicates[i].Value, varSizes + i, true);
                }

                byte* pkPtr = stackalloc byte[keySize];
                EntityId.WriteRaftGroupId(pkPtr, groupId);
                pkPtr[KeyUtil.INDEXCF_INDEXID_POS] = _indexModel.IndexId;

                var writer = new EntityStoreWriter(pkPtr, KeyUtil.INDEXCF_PREFIX_SIZE);
                for (int i = 0; i < _predicates.Length; i++)
                {
                    //注意写入索引键排序标记
                    writer.WriteMember(ref _predicates[i].Value, varSizes, true, _indexModel.Fields[i].OrderByDesc);
                }
                keyPtr = new IntPtr(pkPtr);
            }

            //TODO: 根据是否在事务内走ReadIndex或事务读命令
            var res = await StoreApi.Api.ReadIndexByGetAsync(groupId, keyPtr, (uint)keySize, KeyUtil.INDEXCF_INDEX);
            return res == null ? IndexRow.Empty : new IndexRow(res);
        }
    }
}
