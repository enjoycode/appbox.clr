using System;
using System.Threading.Tasks;
using appbox.Data;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class LoadDesignTree : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            await hub.DesignTree.LoadNodesAsync();
            return hub.DesignTree.Nodes.ToArray();
        }
    }
}
