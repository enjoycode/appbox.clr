using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 前端设计器打开实体模型时获取实体模型信息
    /// </summary>
    sealed class GetEntityModel : IRequestHandler
    {
        public
#if FUTURE
            async
#endif
            Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelId = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Entity, ulong.Parse(modelId));
            if (modelNode == null)
                throw new Exception($"Cannot find EntityModel: {modelId}");

            var model = (EntityModel)modelNode.Model;
            if (model.SysStoreOptions != null)
            {
#if FUTURE
                //注意刷新构建中的索引状态
                await Store.ModelStore.LoadIndexBuildingStatesAsync(modelNode.AppNode.Model, (EntityModel)modelNode.Model);
#endif
            }
            else if (model.SqlStoreOptions != null)
            {
                var storeNode = (DataStoreNode)hub.DesignTree.FindNode(
                    DesignNodeType.DataStoreNode, model.SqlStoreOptions.StoreModelId.ToString());
                if (storeNode == null)
                    throw new Exception($"Cannot find Store: {model.SqlStoreOptions.StoreModelId}");
                model.SqlStoreOptions.DataStoreModel = storeNode.Model; //set cache
            }
            else if (model.CqlStoreOptions != null)
            {
                var storeNode = (DataStoreNode)hub.DesignTree.FindNode(
                    DesignNodeType.DataStoreNode, model.CqlStoreOptions.StoreModelId.ToString());
                if (storeNode == null)
                    throw new Exception($"Cannot find Store: {model.CqlStoreOptions.StoreModelId}");
                model.CqlStoreOptions.DataStoreModel = storeNode.Model; //set cache
            }

#if FUTURE
            return modelNode.Model;
#else
            return Task.FromResult<object>(modelNode.Model);
#endif
        }
    }
}
