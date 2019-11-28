using System;
using appbox.Data;
using appbox.Models;

namespace appbox.Design
{
    sealed class DataStoreRootNode : DesignNode, ITopNode
    {
        public override DesignNodeType NodeType => DesignNodeType.DataStoreRootNode;

        private readonly DesignTree _designTree;
        public override DesignTree DesignTree => _designTree;

        internal DataStoreRootNode(DesignTree designTree)
        {
            _designTree = designTree;
            Text = "DataStore";
        }

        internal DataStoreNode AddModel(DataStoreModel model, DesignHub hub)
        {
            //注意model可能被签出的本地替换掉，所以相关操作必须指向node.Model
            var node = new DataStoreNode(model, hub);
            DesignTree.BindCheckoutInfo(node, model.PersistentState == PersistentState.Detached);
            Nodes.Add(node);
            return node;
        }
    }
}
