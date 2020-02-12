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

            if (!(hub.DesignTree.FindNode(DesignNodeType.DataStoreNode, nodeId) is DataStoreNode node))
                throw new Exception("Can't find node: " + nodeId);

            node.Model.Settings = settings;
            await node.SaveAsync();
            return null;
        }
    }
}
