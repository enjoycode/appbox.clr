using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class CloseDesigner : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var nodeType = (DesignNodeType)args.GetInt32();
            var modelID = args.GetString();

            //Log.Debug($"nodeType={nodeType} modelId={modelID}");

            if (nodeType == DesignNodeType.ServiceModelNode)
            {
                var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
                if (modelNode != null) //可能已被删除了，即由删除节点引发的关闭设计器
                {
                    var fileName = $"{modelNode.AppNode.Model.Name}.Services.{modelNode.Model.Name}.cs";
                    var document = hub.TypeSystem.Workspace.GetOpenedDocumentByName(fileName);
                    if (document != null)
                    {
                        hub.TypeSystem.Workspace.CloseDocument(document.Id);
                    }
                }
            }
            else if (nodeType == DesignNodeType.WorkflowModelNode)
            {
                throw ExceptionHelper.NotImplemented();
                //var sr = modelID.Split('.');
                //var modelNode = hub.DesignTree.FindModelNode(ModelType.Workflow, sr[0], sr[1]);
                //hub.WorkflowDesignService.CloseWorkflowModel(modelNode);
            }
            return Task.FromResult<object>(null);
        }
    }
}
