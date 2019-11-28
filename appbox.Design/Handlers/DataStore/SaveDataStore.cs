using System;
using System.Threading.Tasks;
using appbox.Data;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class SaveDataStore : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var nodeId = args.GetString();
            var settings = args.GetString();

            var node = hub.DesignTree.FindNode(DesignNodeType.DataStoreNode, nodeId) as DataStoreNode;
            if (node == null)
                throw new Exception("Can't find node: " + nodeId);

            node.Model.Settings = settings;
            await node.SaveAsync();
            return null;
        }
    }
}
