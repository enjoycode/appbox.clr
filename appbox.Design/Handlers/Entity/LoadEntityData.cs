using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Store;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 用于前端实体模型设计器数据视图面板加载数据
    /// </summary>
    sealed class LoadEntityData : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelId = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Entity, ulong.Parse(modelId));
            if (modelNode == null)
                throw new Exception($"Cannot find EntityModel: {modelId}");
            var model = (EntityModel)modelNode.Model;
            if (model.StoreOptions == null)
                throw new Exception("DTO can't load data.");
            if (model.PersistentState == PersistentState.Detached)
            {
                //TODO:考虑根据设计时模型生成一条伪记录给前端
                throw new Exception("EntityModel is new, can't load data.");
            }
            if (model.SysStoreOptions != null)
            {
                var q = new TableScan(model.Id);
                var res = await q.Take(20).ToListAsync();
                if (res == null || res.Count == 0)
                    throw new Exception("no record"); //TODO: 同上
                return res;
            }
            if (model.SqlStoreOptions != null)
            {
                var q = new SqlQuery(model.Id);
                return await q.Top(20).ToListAsync();
            }
            throw new NotSupportedException();
        }
    }
}
