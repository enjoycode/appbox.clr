using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewEntityModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            int selectedNodeType = args.GetInt32();
            var selectedNodeId = args.GetString();
            var name = args.GetString();
            var localizedName = args.GetString();
            var storeName = args.GetString();
            var orderByDesc = args.GetBoolean();

            // 验证类名称的合法性
            if (string.IsNullOrEmpty(name))
                throw new Exception("Entity name empty");
            if (!CodeHelper.IsValidIdentifier(name))
                throw new Exception("Entity name invalid");

            //获取选择的节点
            var selectedNode = hub.DesignTree.FindNode((DesignNodeType)selectedNodeType, selectedNodeId);
            if (selectedNode == null)
                throw new Exception("Can't find selected node");

            //根据选择的节点获取合适的插入位置
            var parentNode = hub.DesignTree.FindNewModelParentNode(selectedNode, out uint appId, ModelType.Entity);
            if (parentNode == null)
                throw new Exception("Can't find parent node");
            //判断名称是否已存在
            if (hub.DesignTree.FindModelNodeByName(appId, ModelType.Entity, name) != null)
                throw new Exception("Entity name has exists");

            //判断当前模型根节点有没有签出
            var rootNode = hub.DesignTree.FindModelRootNode(appId, ModelType.Entity);
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
            var modelId = await Store.ModelStore.GenModelIdAsync(appId, ModelType.Entity, ModelLayer.DEV); //TODO:fix Layer
            //根据映射的存储创建相应的实体模型
            EntityModel entityModel;
            if (string.IsNullOrEmpty(storeName))
            {
                throw new NotImplementedException("Create DTO is not implemented.");
            }
            else if (storeName == "Default")
            {
                entityModel = new EntityModel(modelId, name, EntityStoreType.StoreWithMvcc, orderByDesc); //TODO: fix without mvcc
            }
            else
            {
                var storeNode = hub.DesignTree.FindDataStoreNodeByName(storeName);
                if (storeNode == null)
                    throw new Exception($"Can't find sqlstore: {storeName}");
                IEntityStoreOptions storeOptions;
                if (storeNode.Model.Kind == DataStoreKind.Sql)
                    storeOptions = new SqlStoreOptions(storeNode.Model.Id);
                else
                    storeOptions = new CqlStoreOptions(storeNode.Model.Id);
                entityModel = new EntityModel(modelId, name, storeOptions);
            }

            //if (!string.IsNullOrWhiteSpace(localizedName))
            //entityModel.LocalizedName.Value = localizedName;

            //添加至设计树
            var node = new ModelNode(entityModel, hub);
            var insertIndex = parentNode.Nodes.Add(node);
            // 设置文件夹
            if (parentNode.NodeType == DesignNodeType.FolderNode)
                entityModel.FolderId = ((FolderNode)parentNode).Folder.Id;
            // 添加至根节点索引内
            rootNode.AddModelIndex(node);

            // 设为签出状态
            node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, entityModel.Version,
                                                 hub.Session.Name, hub.Session.LeafOrgUnitID);

            //保存至Staged
            await node.SaveAsync(null);
            // 新建RoslynDocument
            await hub.TypeSystem.CreateModelDocumentAsync(node);

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
