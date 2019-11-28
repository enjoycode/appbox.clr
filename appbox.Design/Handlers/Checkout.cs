using System;
using System.Threading.Tasks;
using appbox.Data;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class Checkout : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var nodeType = (DesignNodeType)args.GetInt32();
            var nodeID = args.GetString();

            var node = hub.DesignTree.FindNode(nodeType, nodeID);
            if (node == null)
                throw new Exception($"Can't find DesignNode: {nodeID}");

            var modelNode = node as ModelNode;
            if (modelNode != null)
            {
                var curVersion = modelNode.Model.Version;
                bool checkoutOk = await modelNode.Checkout();
                if (!checkoutOk)
                    throw new Exception($"Can't checkout ModelNode: {nodeID}");
                if (curVersion != modelNode.Model.Version)
                    return true; //返回True表示模型已变更，用于前端刷新
            }
            else if (node.NodeType == DesignNodeType.ModelRootNode)
            {
                bool checkoutOk = await node.Checkout();
                if (!checkoutOk)
                    throw new Exception("Can't checkout ModelRootNode");
                return true; //TODO:暂返回需要更新
            }
            else if (node.NodeType == DesignNodeType.DataStoreNode)
            {
                bool checkoutOk = await node.Checkout();
                if (!checkoutOk)
                    throw new Exception("Can't checkout DataStoreNode");
                return false;
            }
            else
            {
                throw new Exception("无法签出此类型的节点");
            }

            return false;
        }
    }
}
