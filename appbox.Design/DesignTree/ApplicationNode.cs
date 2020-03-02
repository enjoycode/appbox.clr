using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Models;

namespace appbox.Design
{
    public sealed class ApplicationNode : DesignNode
    {
        public override DesignNodeType NodeType => DesignNodeType.ApplicationNode;

        internal ApplicationModel Model { get; private set; }

        internal ApplicationNode(DesignTree tree, ApplicationModel model)
        {
            Model = model;
            Text = model.Name;

            //添加各模型类型的根节点
            var modelRoot = new ModelRootNode(ModelType.Entity);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Service);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.View);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Workflow);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Report);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Enum);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Event);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            modelRoot = new ModelRootNode(ModelType.Permission);
            Nodes.Add(modelRoot);
            tree.BindCheckoutInfo(modelRoot, false);

            //添加BlobStoreNode
            var blobNode = new BlobStoreNode();
            Nodes.Add(blobNode);
        }

        internal ModelRootNode FindModelRootNode(ModelType modelType)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] is ModelRootNode modelRootNode && modelRootNode.TargetType == modelType)
                    return modelRootNode;
            }
            return null;
        }

        internal FolderNode FindFolderNode(Guid folderID)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] is ModelRootNode modelRootNode)
                {
                    var res = modelRootNode.FindFolderNode(folderID);
                    if (res != null)
                        return res;
                }
            }
            return null;
        }

        /// <summary>
        /// 签入当前应用节点下所有子节点
        /// </summary>
        internal void CheckinAllNodes()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] is ModelRootNode modelRootNode)
                    modelRootNode.CheckinAllNodes();
            }
        }

        internal IList<ModelNode> GetAllModelNodes()
        {
            var list = new List<ModelNode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] is ModelRootNode modelRootNode)
                    list.AddRange(modelRootNode.GetAllModelNodes());
            }
            return list;
        }

    }

}
