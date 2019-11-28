using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class OpenServiceModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception($"Cannot find service model: {modelID}");

            //先判断是否已经打开，是则先关闭，主要用于签出后重新加载
            if (hub.TypeSystem.Workspace.IsDocumentOpen(modelNode.RoslynDocumentId))
                hub.TypeSystem.Workspace.CloseDocument(modelNode.RoslynDocumentId);

            hub.TypeSystem.Workspace.OpenDocument(modelNode.RoslynDocumentId, false);
            var doc = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(modelNode.RoslynDocumentId);
            var sourceText = await doc.GetTextAsync();
            return sourceText.ToString();
        }
    }
}
