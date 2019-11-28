using System;
using System.Collections.Generic;
using System.Linq;
using appbox.Data;
using appbox.Models;

namespace appbox.Design
{
    /// <summary>
    /// 模型类型根节点
    /// </summary>
    public sealed class ModelRootNode : DesignNode
    {
        #region ====Fields & Properties====
        public override DesignNodeType NodeType => DesignNodeType.ModelRootNode;

        internal override int SortNo
        {
            get
            {
                switch (TargetType)
                {
                    case ModelType.Entity:
                        return 0;
                    case ModelType.Service:
                        return 1;
                    case ModelType.View:
                        return 2;
                    case ModelType.Workflow:
                        return 3;
                    case ModelType.Report:
                        return 4;
                    case ModelType.Enum:
                        return 5;
                    case ModelType.Event:
                        return 6;
                    case ModelType.Permission:
                        return 7;
                    default:
                        return 0;
                }
            }
        }

        internal uint AppID => ((ApplicationNode)Parent).Model.Id;

        public override string ID => $"{AppID}-{(int)TargetType}";

        public override string CheckoutInfoTargetID => ID;

        /// <summary>
        /// 当前类型下的根文件夹
        /// </summary>
        public ModelFolder RootFolder { get; internal set; }

        /// <summary>
        /// 文件夹字典表
        /// </summary>
        private readonly Dictionary<Guid, FolderNode> _folders = new Dictionary<Guid, FolderNode>();

        /// <summary>
        /// 模型字典表 key= ModelBase.Name
        /// </summary>
        private readonly Dictionary<ulong, ModelNode> _models = new Dictionary<ulong, ModelNode>();

        internal ModelType TargetType { get; private set; }

        internal string FullName => $"{Parent.Text}.{Text}";
        #endregion

        #region ====Ctor====
        internal ModelRootNode(ModelType targetType)
        {
            TargetType = targetType;
            Text = CodeHelper.GetPluralStringOfModelType(targetType);
        }
        #endregion

        #region ====Add & Remove SubNode Methods====
        /// <summary>
        /// 用于新建时添加至字典表
        /// </summary>
        internal void AddFolderIndex(FolderNode node)
        {
            _folders.Add(node.Folder.Id, node);
        }

        /// <summary>
        /// 删除并移除字典表中的对应键
        /// </summary>
        internal void RemoveFolder(FolderNode node)
        {
            node.Parent.Nodes.Remove(node);
            if (_folders.ContainsKey(node.Folder.Id))
                _folders.Remove(node.Folder.Id);
        }

        /// <summary>
        /// 仅用于设计树从顶级开始递归添加文件夹节点
        /// </summary>
        internal void AddFolder(ModelFolder folder, DesignNode parent = null)
        {
            DesignNode parentNode = this;
            if (folder.Parent != null)
            {
                var node = new FolderNode(folder);
                parentNode = node;
                //不再检查本地有没有挂起的修改,由DesignTree加载时处理好
                if (parent == null)
                    Nodes.Add(node);
                else
                    parent.Nodes.Add(node);

                _folders.Add(folder.Id, node);
            }
            else
            {
                RootFolder = folder;
            }

            if (folder.HasChilds)
            {
                foreach (var item in folder.Childs)
                {
                    AddFolder(item, parentNode);
                }
            }
        }

        /// <summary>
        /// 用于新建时添加至字典表
        /// </summary>
        internal void AddModelIndex(ModelNode node)
        {
            _models.Add(node.Model.Id, node);
        }

        /// <summary>
        /// 删除并移除字典表中对应的键
        /// </summary>
        internal void RemoveModel(ModelNode node)
        {
            node.Parent.Nodes.Remove(node);
            if (_models.ContainsKey(node.Model.Id))
                _models.Remove(node.Model.Id);
        }

        /// <summary>
        /// 仅用于加载设计树时添加节点并绑定签出信息
        /// </summary>
        internal ModelNode AddModel(ModelBase model)
        {
            //注意: 1.不在这里创建相应的RoslynDocument,因为可能生成虚拟代码时找不到引用的模型，待加载完整个树后再创建
            //     2.model可能被签出的本地替换掉，所以相关操作必须指向node.Model
            var node = new ModelNode(model, DesignTree.DesignHub);
            DesignTree.BindCheckoutInfo(node, model.PersistentState == PersistentState.Detached);

            if (node.Model.FolderId.HasValue)
            {
                if (_folders.TryGetValue(node.Model.FolderId.Value, out FolderNode folderNode))
                    folderNode.Nodes.Add(node);
                else
                    Nodes.Add(node);
            }
            else
            {
                Nodes.Add(node);
            }
            _models.Add(node.Model.Id, node);
            return node;
        }
        #endregion

        #region ====Find Methods====
        public ModelNode FindModelNodeByName(string name)
        {
            foreach (var node in _models.Values)
            {
                if (node.Model.Name == name)
                    return node;
            }
            return null;
        }

        public ModelNode FindModelNode(ulong modelId)
        {
            _models.TryGetValue(modelId, out ModelNode node);
            return node;
        }

        public FolderNode FindFolderNode(Guid folderID)
        {
            _folders.TryGetValue(folderID, out FolderNode node);
            return node;
        }

        public ModelNode[] GetAllModelNodes()
        {
            return _models.Values.ToArray();
        }
        #endregion

        #region ====Checkin & Checkout Methods====
        internal void CheckinAllNodes()
        {
            //定义待删除模型节点列表
            List<ModelNode> ls = new List<ModelNode>();

            //签入模型根节点，文件夹的签出信息同模型根节点
            if (IsCheckoutByMe)
                CheckoutInfo = null;

            //签入所有模型节点
            foreach (ModelNode n in _models.Values)
            {
                if (n.IsCheckoutByMe)
                {
                    //判断是否待删除的节点
                    if (n.Model.PersistentState == PersistentState.Deleted)
                    {
                        ls.Add(n);
                    }
                    else
                    {
                        n.CheckoutInfo = null;
                        n.Model.Version = n.Model.Version + 1;
                        n.Model.AcceptChanges(); //注意：模型接受更改时，模型关联的资源自动接受更改
                    }
                }
            }

            //TODO:移除已删除的文件夹节点
            //开始移除待删除的模型节点
            for (int i = 0; i < ls.Count; i++)
            {
                //先移除索引
                _models.Remove(ls[i].Model.Id);
                //再移除节点
                ls[i].Parent.Nodes.Remove(ls[i]);
            }
        }
        #endregion
    }

}
