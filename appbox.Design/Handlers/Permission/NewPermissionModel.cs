using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewPermissionModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            //读取参数
            int selectedNodeType = args.GetInt32();
            string selectedNodeId = args.GetString();
            string newname = args.GetString();

            //先判断名称有效性
            if (string.IsNullOrEmpty(newname))
                throw new Exception("名称不能为空");
            if (!CodeHelper.IsValidIdentifier(newname))
                throw new Exception("名称包含无效字符");

            //获取选择的节点
            var selectedNode = hub.DesignTree.FindNode((DesignNodeType)selectedNodeType, selectedNodeId);
            if (selectedNode == null)
                throw new Exception("无法找到当前节点");
            //根据选择的节点获取合适的插入位置
            var parentNode = hub.DesignTree.FindNewModelParentNode(selectedNode, out uint appId, ModelType.Permission);
            if (parentNode == null)
                throw new Exception("无法找到当前节点的上级节点");
            //判断名称是否已存在
            if (hub.DesignTree.FindModelNodeByName(appId, ModelType.Permission, newname) != null)
                throw new Exception("Name has exists");

            //判断当前模型根节点有没有签出
            var rootNode = hub.DesignTree.FindModelRootNode(appId, ModelType.Permission);
            bool rootNodeHasCheckout = rootNode.IsCheckoutByMe;
            if (!await rootNode.Checkout())
                throw new Exception($"Can't checkout: {rootNode.FullName}");
            ////注意:需要重新引用上级文件夹节点，因自动签出上级节点可能已重新加载
            //if (!rootNodeHasCheckout && parentNode.NodeType == DesignNodeType.FolderNode)
            //{
            //    parentNode = rootNode.FindFolderNode(((FolderNode)parentNode).Folder.ID);
            //    if (parentNode == null)
            //        throw new Exception("上级节点已不存在，请刷新重试");
            //}

            //生成模型标识号并新建模型及节点
            var modelId = await Store.ModelStore.GenModelIdAsync(appId, ModelType.Permission, ModelLayer.DEV); //TODO:fix Layer
            var model = new PermissionModel(modelId, newname);
            var node = new ModelNode(model, hub);
            //添加至设计树
            var insertIndex = parentNode.Nodes.Add(node);
            //设置文件夹
            if (parentNode.NodeType == DesignNodeType.FolderNode)
                model.FolderId = ((FolderNode)parentNode).Folder.Id;
            //添加至根节点索引内
            rootNode.AddModelIndex(node);

            //设为签出状态
            node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, model.Version,
                                                 hub.Session.Name, hub.Session.LeafOrgUnitID);

            //保存至本地
            await node.SaveAsync(null);
            //新建RoslynDocument
            await hub.TypeSystem.CreateModelDocumentAsync(node);

            return new NewNodeResult()
            {
                ParentNodeType = (int)parentNode.NodeType,
                ParentNodeID = parentNode.ID,
                NewNode = node,
                RootNodeID = rootNodeHasCheckout ? null : rootNode.ID,
                InsertIndex = insertIndex
            };
        }
    }
}
