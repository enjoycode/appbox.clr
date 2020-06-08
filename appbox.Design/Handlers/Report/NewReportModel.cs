using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewReportModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            int selectedNodeType = args.GetInt32();
            string selectedNodeId = args.GetString();
            string name = args.GetString();

            // 验证类名称的合法性
            if (string.IsNullOrEmpty(name) || !CodeHelper.IsValidIdentifier(name))
                throw new Exception("Report name invalid");
            // 获取选择的节点
            var selectedNode = hub.DesignTree.FindNode((DesignNodeType)selectedNodeType, selectedNodeId);
            if (selectedNode == null)
                throw new Exception("Can't find selected node");

            var parentNode = hub.DesignTree.FindNewModelParentNode(selectedNode, out uint appId, ModelType.Report);
            if (parentNode == null)
                throw new Exception("Can't find parent node");
            //判断名称是否已存在
            if (hub.DesignTree.FindModelNodeByName(appId, ModelType.Report, name) != null)
                throw new Exception("Report name has exists");

            //判断当前模型根节点有没有签出
            var rootNode = hub.DesignTree.FindModelRootNode(appId, ModelType.Report);
            bool rootNodeHasCheckout = rootNode.IsCheckoutByMe;
            if (!await rootNode.Checkout())
                throw new Exception($"Can't checkout: {rootNode.FullName}");
            ////注意:需要重新引用上级文件夹节点，因自动签出上级节点可能已重新加载(签出模型根不再重新加载)
            //if (!rootNodeHasCheckout && parentNode.NodeType == DesignNodeType.FolderNode)
            //{
            //    parentNode = rootNode.FindFolderNode(((FolderNode)parentNode).Folder.ID);
            //    if (parentNode == null)
            //        throw new Exception("上级节点已不存在，请刷新重试");
            //}

            //生成模型标识号并新建模型及节点
            var modelId = await Store.ModelStore.GenModelIdAsync(appId, ModelType.Report, ModelLayer.DEV); //TODO:fix Layer
            var model = new ReportModel(modelId, name);
            var node = new ModelNode(model, hub);
            //添加至设计树
            var insertIndex = parentNode.Nodes.Add(node);
            //设置文件夹
            if (parentNode.NodeType == DesignNodeType.FolderNode)
                model.FolderId = ((FolderNode)parentNode).Folder.Id;
            // 添加至根节点索引内
            rootNode.AddModelIndex(node);

            //设为签出状态
            node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, model.Version,
                hub.Session.Name, hub.Session.LeafOrgUnitID);

            //保存至Staged
            var emptyReportDfn = Resources.LoadStringResource("Resources.EmptyReport.json");
            await node.SaveAsync(new object[] { emptyReportDfn });

            return new NewNodeResult
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
