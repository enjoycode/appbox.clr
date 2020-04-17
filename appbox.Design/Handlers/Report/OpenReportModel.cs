using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class OpenReportModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Report, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception($"Cannot find report model: {modelID}");

            if (modelNode.IsCheckoutByMe)
            {
                var code = await StagedService.LoadReportCodeAsync(modelNode.Model.Id);
                if (code != null) return code;
            }

            return await Store.ModelStore.LoadReportCodeAsync(modelNode.Model.Id);
        }
    }
}
