using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewFolder : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            //读取参数
            int selectedNodeType = args.GetInt32();
            string selectedNodeId = args.GetString();
            string name = args.GetString();

            if (string.IsNullOrEmpty(name))
                throw new Exception("名称不能为空");

            //获取选择的节点
            var selectedNode = hub.DesignTree.FindNode((DesignNodeType)selectedNodeType, selectedNodeId);
            if (selectedNode == null)
                throw new Exception("无法找到当前节点");
            //根据选择的节点获取合适的插入位置
            var parentNode = hub.DesignTree.FindNewFolderParentNode(selectedNode, out uint appId, out ModelType modelType);
            if (parentNode == null)
                throw new Exception("无法找到上级节点");
            if (parentNode.Nodes.Exists(t => t.NodeType == DesignNodeType.FolderNode && t.Text == name))
                throw new Exception("当前目录下已存在同名文件夹");

            //判断当前模型根节点有没有签出
            var rootNode = hub.DesignTree.FindModelRootNode(appId, modelType);
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

            // 判断选择节点即parentNode是文件夹还是模型根节点
            ModelFolder model = null;
            if (parentNode.NodeType == DesignNodeType.FolderNode)
            {
                model = new ModelFolder(((FolderNode)parentNode).Folder, name);
            }
            else if (parentNode.NodeType == DesignNodeType.ModelRootNode)
            {
                //判断是否存在根文件夹
                if (rootNode.RootFolder == null)
                    rootNode.RootFolder = new ModelFolder(appId, rootNode.TargetType);
                model = new ModelFolder(rootNode.RootFolder, name);
            }
            else
                throw new Exception("不允许在此节点创建文件夹");

            var node = new FolderNode(model);
            //添加至设计树
            var insertIndex = parentNode.Nodes.Add(node);
            // 添加至根节点索引内
            rootNode.AddFolderIndex(node);
            // 保存到本地
            await node.SaveAsync();

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
