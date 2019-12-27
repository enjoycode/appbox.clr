using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;
using appbox.Store;
using appbox.Serialization;
using System.Collections.Generic;

namespace appbox.Design
{
    /// <summary>
    /// 管理设计时临时保存的尚未发布的模型及相关代码
    /// </summary>
    static class StagedService
    {

        /// <summary>
        /// 保存Staged模型
        /// </summary>
        internal static Task SaveModelAsync(ModelBase model)
        {
            var data = BinSerializer.Serialize(model, false, null);
            return SaveAsync(StagedType.Model, model.Id.ToString(), data);
        }

        /// <summary>
        /// 保存模型类型的根目录
        /// </summary>
        internal static Task SaveFolderAsync(ModelFolder folder)
        {
            if (folder.Parent != null)
                throw new InvalidOperationException("仅允许保存模型类型的根目录");
            var data = BinSerializer.Serialize(folder, false, null);
            return SaveAsync(StagedType.Folder, $"{folder.AppId}-{(byte)folder.TargetModelType}" /*不要使用folder.Id*/, data);
        }

        /// <summary>
        /// 专用于保存服务模型代码
        /// </summary>
        internal static Task SaveServiceCodeAsync(ulong modelId, string sourceCode)
        {
            var data = ModelCodeUtil.EncodeServiceCode(sourceCode, null);
            return SaveAsync(StagedType.SourceCode, modelId.ToString(), data);
        }

        /// <summary>
        /// 专用于加载服务模型代码
        /// </summary>
        internal static async ValueTask<string> LoadServiceCode(ulong serviceModelId)
        {
            var developerID = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;

#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID &
                     q.GetString(Consts.STAGED_MODELID_ID) == serviceModelId.ToString() &
                     q.GetByte(Consts.STAGED_TYPE_ID) == (byte)StagedType.SourceCode);
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["DeveloperId"] == developerID &
                q.T["ModelId"] == serviceModelId.ToString() &
                q.T["Type"] == (byte)StagedType.SourceCode);
#endif
            var res = await q.ToListAsync();
            if (res == null || res.Count == 0)
                return null;

            var data = res[0].GetBytes(Consts.STAGED_DATA_ID);
            ModelCodeUtil.DecodeServiceCode(data, out string sourceCode, out string declareCode);
            return sourceCode;
        }

        /// <summary>
        /// 专用于保存视图模型代码
        /// </summary>
        internal static Task SaveViewCodeAsync(ulong modelId, string templateCode, string scriptCode, string styleCode)
        {
            var data = ModelCodeUtil.EncodeViewCode(templateCode, scriptCode, styleCode);
            return SaveAsync(StagedType.SourceCode, modelId.ToString(), data);
        }

        internal static async Task<ValueTuple<bool, string, string, string>> LoadViewCodeAsync(ulong modelId)
        {
            var developerID = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;

#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID &
                     q.GetString(Consts.STAGED_MODELID_ID) == modelId.ToString() &
                     q.GetByte(Consts.STAGED_TYPE_ID) == (byte)StagedType.SourceCode);
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["DeveloperId"] == developerID &
                q.T["ModelId"] == modelId.ToString() &
                q.T["Type"] == (byte)StagedType.SourceCode);
#endif
            var res = await q.ToListAsync();
            if (res == null || res.Count == 0)
                return ValueTuple.Create<bool, string, string, string>(false, null, null, null);

            var data = res[0].GetBytes(Consts.STAGED_DATA_ID);
            ModelCodeUtil.DecodeViewCode(data, out string templateCode, out string scriptCode, out string styleCode);
            return ValueTuple.Create(true, templateCode, scriptCode, styleCode);
        }

        internal static Task SaveViewRuntimeCodeAsync(ulong modelId, string runtimeCode)
        {
            if (string.IsNullOrEmpty(runtimeCode))
                return Task.CompletedTask;

            var data = ModelCodeUtil.EncodeViewRuntimeCode(runtimeCode);
            return SaveAsync(StagedType.ViewRuntimeCode, modelId.ToString(), data);
        }

        internal static async ValueTask<string> LoadViewRuntimeCode(ulong viewModelId)
        {
            var developerID = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;

#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID &
                     q.GetString(Consts.STAGED_MODELID_ID) == viewModelId.ToString() &
                     q.GetByte(Consts.STAGED_TYPE_ID) == (byte)StagedType.ViewRuntimeCode);
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["DeveloperId"] == developerID &
                q.T["ModelId"] == viewModelId.ToString() &
                q.T["Type"] == (byte)StagedType.ViewRuntimeCode);
#endif
            var res = await q.ToListAsync();
            if (res == null || res.Count == 0)
                return null;

            var data = res[0].GetBytes(Consts.STAGED_DATA_ID);
            ModelCodeUtil.DecodeViewRuntimeCode(data, out string runtimeCode);
            return runtimeCode;
        }

        private static async Task SaveAsync(StagedType type, string modelId, byte[] data)
        {
            var developerID = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_STAGED_MODEL_ID);

            //TODO:使用SelectForUpdate or BatchDelete

#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            byte typeValue = (byte)type;
            q.Filter(q.GetByte(Consts.STAGED_TYPE_ID) == typeValue &
                q.GetString(Consts.STAGED_MODELID_ID) == modelId &
                q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID);

            var txn = await Transaction.BeginAsync();
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["Type"] == (byte)type &
                q.T["ModelId"] == modelId &
                q.T["DeveloperId"] == developerID);

            var txn = SqlStore.Default.BeginTransaction();
#endif

            var res = await q.ToListAsync();
            if (res != null && res.Count > 0)
            {
                //TODO:*****临时先删除再重新插入
                for (int i = 0; i < res.Count; i++)
                {
#if FUTURE
                    await EntityStore.DeleteEntityAsync(model, res[i].Id, txn);
#else
                    await SqlStore.Default.DeleteAsync(res[i], txn);
#endif
                }
            }

            var obj = new Entity(model);
            obj.SetByte(Consts.STAGED_TYPE_ID, (byte)type);
            obj.SetString(Consts.STAGED_MODELID_ID, modelId);
            obj.SetGuid(Consts.STAGED_DEVELOPERID_ID, developerID);
            obj.SetBytes(Consts.STAGED_DATA_ID, data);
#if FUTURE
            await EntityStore.InsertEntityAsync(obj, txn);
            await txn.CommitAsync();
#else
            await SqlStore.Default.InsertAsync(obj, txn);
            txn.Commit();
#endif
        }

        /// <summary>
        /// 加载挂起项目
        /// </summary>
        /// <param name="onlyModelsAndFolders">true用于DesignTree加载; false用于发布时加载</param>
        internal static async Task<StagedItems> LoadStagedAsync(bool onlyModelsAndFolders)
        {
            //TODO:考虑用于DesignTree加载时连服务模型的代码一并加载
            var developerID = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;

#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            if (onlyModelsAndFolders)
                q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID &
                         q.GetByte(Consts.STAGED_TYPE_ID) <= (byte)StagedType.Folder);
            else
                q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == developerID);
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            if (onlyModelsAndFolders)
                q.Where(q.T["DeveloperId"] == developerID & q.T["Type"] == (byte)StagedType.Folder);
            else
                q.Where(q.T["DeveloperId"] == developerID);
#endif
            var res = await q.ToListAsync();
            return new StagedItems(res);
        }

        /// <summary>
        /// 发布时删除当前会话下所有挂起
        /// </summary>
        internal static async Task DeleteStagedAsync(
#if FUTURE
            Transaction txn
#else
            System.Data.Common.DbTransaction txn
#endif
            )
        {
            //TODO:****暂查询再删除, use BatchDelete
            var devId = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_STAGED_MODEL_ID);
#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == devId);
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["DeveloperId"] == devId);
#endif
            var list = await q.ToListAsync();
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
#if FUTURE
                await EntityStore.DeleteEntityAsync(model, list[i].Id, txn);
#else
                await SqlStore.Default.DeleteAsync(list[i], txn);
#endif
            }
        }

        /// <summary>
        /// 删除挂起的模型及相关
        /// </summary>
        internal static async Task DeleteModelAsync(ulong modelId)
        {
            //TODO:***暂查询再删除
            var devId = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
            //删除模型
#if FUTURE
            var q = new TableScan(Consts.SYS_STAGED_MODEL_ID);
            q.Filter(q.GetGuid(Consts.STAGED_DEVELOPERID_ID) == devId
                & q.GetString(Consts.STAGED_MODELID_ID) == modelId.ToString());
#else
            var q = new SqlQuery(Consts.SYS_STAGED_MODEL_ID);
            q.Where(q.T["DeveloperId"] == devId &
                q.T["ModelId"] == modelId.ToString());
#endif
            var list = await q.ToListAsync();
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
#if FUTURE
                await EntityStore.DeleteAsync(list[i]);
#else
                await SqlStore.Default.DeleteAsync(list[i], null);
#endif
            }
        }
    }

    /// <summary>
    /// 用于区分Staged.Data内存储的数据类型
    /// </summary>
    enum StagedType : byte
    {
        Model = 0,      //模型序列化数据
        Folder,         //文件夹
        SourceCode,     //服务模型或视图模型的源代码 //TODO:考虑按类型分开
        ViewRuntimeCode //仅用于视图模型前端编译好的运行时脚本代码
    }

    sealed class StagedItems
    {
        internal object[] Items { get; }

        internal StagedItems(IList<Entity> staged)
        {
            if (staged != null && staged.Count > 0)
            {
                Items = new object[staged.Count];
                StagedType type;
                for (int i = 0; i < staged.Count; i++)
                {
                    var data = staged[i].GetBytes(Consts.STAGED_DATA_ID);

                    type = (StagedType)staged[i].GetByte(Consts.STAGED_TYPE_ID);
                    switch (type)
                    {
                        case StagedType.Model:
                        case StagedType.Folder:
                            Items[i] = BinSerializer.Deserialize(data, null);
                            break;
                        case StagedType.SourceCode:
                            {
                                ulong modelId = ulong.Parse(staged[i].GetString(Consts.STAGED_MODELID_ID)); //TODO:fix 
                                Items[i] = new StagedSourceCode { ModelId = modelId, CodeData = data };
                            }
                            break;
                        case StagedType.ViewRuntimeCode:
                            {
                                ulong modelId = ulong.Parse(staged[i].GetString(Consts.STAGED_MODELID_ID)); //TODO:fix 
                                Items[i] = new StagedViewRuntimeCode { ModelId = modelId, CodeData = data };
                            }
                            break;
                        default:
                            throw ExceptionHelper.NotImplemented();
                    }
                }
            }
        }

        internal ModelBase[] FindNewModels()
        {
            var list = new List<ModelBase>();
            if (Items != null)
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    if (Items[i] is ModelBase m && m.PersistentState == PersistentState.Detached)
                        list.Add(m);
                }
            }
            return list.ToArray();
        }

        internal ModelBase FindModel(ulong modelId)
        {
            if (Items != null)
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    if (Items[i] is ModelBase m && m.Id == modelId)
                        return m;
                }
            }
            return null;
        }

        /// <summary>
        /// 用挂起的文件夹更新从存储加载的文件夹
        /// </summary>
        internal void UpdateFolders(List<ModelFolder> storedFolders)
        {
            if (Items == null) return;
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] is ModelFolder folder)
                {
                    var index = storedFolders.FindIndex(t => t.AppId == folder.AppId && t.TargetModelType == folder.TargetModelType);
                    if (index < 0)
                        storedFolders.Add(folder);
                    else
                        storedFolders[index] = folder;
                }
            }
        }

        /// <summary>
        /// 从存储加载的模型中移除已删除的
        /// </summary>
        internal void RemoveDeletedModels(List<ModelBase> storedModels)
        {
            if (Items == null || Items.Length == 0)
                return;

            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] is ModelBase m && m.PersistentState == PersistentState.Deleted)
                {
                    storedModels.RemoveAll(t => t.Id == m.Id);
                }
            }
        }

        internal sealed class StagedSourceCode
        {
            public ulong ModelId;
            public byte[] CodeData;
        }

        internal sealed class StagedViewRuntimeCode
        {
            public ulong ModelId;
            public byte[] CodeData;
        }
    }

}
