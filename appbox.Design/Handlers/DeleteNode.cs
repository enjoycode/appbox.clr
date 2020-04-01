using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 删除模型、文件夹或整个应用
    /// </summary>
    sealed class DeleteNode : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            int selectedNodeType = args.GetInt32();
            string selectedNodeId = args.GetString();
            var deleteNode = hub.DesignTree.FindNode((DesignNodeType)selectedNodeType, selectedNodeId);
            if (deleteNode == null)
                throw new Exception("Delete target not exists.");
            if (!(deleteNode is ModelNode || deleteNode is ApplicationNode
                || (deleteNode.NodeType == DesignNodeType.FolderNode && deleteNode.Nodes.Count == 0)))
                throw new Exception("Can not delete it.");

            DesignNode rootNode = null;
            if (deleteNode is ModelNode modelNode)
                rootNode = await DeleteModelNode(hub, modelNode);
            else if (deleteNode is ApplicationNode appNode)
                await DeleteApplicationNode(hub, appNode);
            else
                throw ExceptionHelper.NotImplemented(); //rootNode = DeleteFolderNode(hub, deleteNode);

            //注意：返回rootNode.ID用于前端重新刷新模型根节点
            return rootNode == null ? string.Empty : rootNode.ID;
        }

        private static async Task<DesignNode> DeleteModelNode(DesignHub hub, ModelNode node)
        {
            // 查找ModelRootNode
            var rootNode = hub.DesignTree.FindModelRootNode(node.Model.AppId, node.Model.ModelType);
            bool rootNodeHasCheckout = rootNode.IsCheckoutByMe;
            // 尝试签出模型节点及根节点
            bool nodeCheckout = await node.Checkout();
            bool rootCheckout = await rootNode.Checkout();
            if (!nodeCheckout || !rootCheckout)
                throw new Exception("Can't checkout nodes.");
            // 注意：如果自动签出了模型根节点，当前选择的节点需要重新指向，因为Node.Checkout()时已重新加载
            if (!rootNodeHasCheckout)
                node = rootNode.FindModelNode(node.Model.Id);
            if (node == null) //可能已不存在
                throw new Exception("Delete target not exists, please refresh.");
            // 判断当前节点所属层是否是系统层
            if (node.Model.ModleLayer == ModelLayer.SYS)
                throw new Exception("Can't delete system model.");
            var model = node.Model;
            // 查找引用项
            var usages = await RefactoringService.FindModelReferencesAsync(hub, model.ModelType,
                                                            node.AppNode.Model.Name, model.Name);
            if (usages != null && usages.Count > 0)
            {
                //注意排除自身引用
                usages = usages.Where(u => !(u.ModelNode.Model.Id  == model.Id)).ToArray();
                if (usages.Count > 0)
                {
#if DEBUG
                    foreach (var item in usages)
                    {
                        Log.Warn(item.ToString());
                    }
#endif
                    throw new Exception("Has usages, Can't delete it.");
                }
            }

            // 判断当前模型是否已持久化到数据库中
            if (model.PersistentState == PersistentState.Detached)
            {
                await StagedService.DeleteModelAsync(model.Id);
            }
            else
            {
                model.MarkDeleted();
                await node.SaveAsync(null);
            }
            // 移除对应节点
            rootNode.RemoveModel(node);
            // 删除Roslyn相关
            RemoveRoslynFromModelNode(hub, node);

            return rootNodeHasCheckout ? null : rootNode;
        }

        private static async Task DeleteApplicationNode(DesignHub hub, ApplicationNode appNode)
        {
            //TODO:*****暂简单实现，待实现: 签出所有子节点，判断有无其他应用的引用
            //TODO:考虑删除整个应用前自动导出备份

            //先组包用现有PublishService发布(删除)
            var pkg = new PublishPackage();
            var modelNodes = appNode.GetAllModelNodes();
            foreach (var modelNode in modelNodes)
            {
                if (modelNode.Model.PersistentState != PersistentState.Detached)
                {
                    modelNode.Model.MarkDeleted();
                    pkg.Models.Add(modelNode.Model);
                    //不用加入需要删除的相关代码及组件
                }

                //删除所有Roslyn相关
                RemoveRoslynFromModelNode(hub, modelNode);
            }
            //加入待删除的根级文件夹
            var rootFolders = appNode.GetAllRootFolders();
            foreach (var rootFolder in rootFolders)
            {
                rootFolder.IsDeleted = true;
                pkg.Folders.Add(rootFolder);
            }
            //TODO:暂使用PublishService.PublishAsync，且与删除ApplicationModel非事务
            await PublishService.PublishAsync(hub, pkg, $"Delete Application: {appNode.Model.Name}");
            //删除ApplicationModel
            await Store.ModelStore.DeleteApplicationAsync(appNode.Model);
            //TODO:清理Staged(只清理当前删除的App相关的)
            //最后从设计树移除ApplicationNode
            hub.DesignTree.AppRootNode.Nodes.Remove(appNode);
        }

        private static void RemoveRoslynFromModelNode(DesignHub hub, ModelNode node)
        {
            if (node.RoslynDocumentId != null)
                hub.TypeSystem.RemoveDocument(node.RoslynDocumentId);
            if (node.AsyncProxyDocumentId != null)
                hub.TypeSystem.RemoveDocument(node.AsyncProxyDocumentId);
            if (node.ServiceProjectId != null) //注意：服务模型移除整个虚拟项目
                hub.TypeSystem.RemoveServiceProject(node.ServiceProjectId);
        }
    }
}
