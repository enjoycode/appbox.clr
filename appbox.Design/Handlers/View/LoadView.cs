using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class LoadView : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString(); // eg: sys.CustomerListView
            var sr = modelID.Split('.');
            var app = hub.DesignTree.FindApplicationNodeByName(sr[0]);
            var node = hub.DesignTree.FindModelNodeByName(app.Model.Id, ModelType.View, sr[1]);
            if (node == null)
                throw new Exception("Cannot found view node: " + modelID);

            var modelNode = node as ModelNode;
            if (modelNode == null)
                throw new Exception("Cannot found view model: " + modelID);

            string runtimeCode = null;
            if (modelNode.IsCheckoutByMe)
            {
                runtimeCode = await StagedService.LoadViewRuntimeCode(modelNode.Model.Id);
            }
            if (string.IsNullOrEmpty(runtimeCode))
            {
                runtimeCode = await Store.ModelStore.LoadViewAssemblyAsync(modelID);
            }

            return runtimeCode;
        }
    }
}
