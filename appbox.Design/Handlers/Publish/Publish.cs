using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// IDE发布变更的模型包
    /// </summary>
    sealed class Publish : IRequestHandler
    {

        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string commitMessage = args.GetString();

            if (hub.PendingChanges == null || hub.PendingChanges.Length == 0)
                return null;

            //将PendingChanges转为PublishPackage
            var package = new PublishPackage();
            for (int i = 0; i < hub.PendingChanges.Length; i++)
            {
                var change = hub.PendingChanges[i];
                switch(change)
                {
                    case ModelBase model:
                        package.Models.Add(model); break;
                    case ModelFolder folder:
                        package.Folders.Add(folder); break;
                    case StagedItems.StagedSourceCode code:
                        package.SourceCodes.Add(code.ModelId, code.CodeData); break;
                    case StagedItems.StagedViewRuntimeCode viewAsm:
                        {
                            //先找到名称
                            var viewModelNode = hub.DesignTree.FindModelNode(ModelType.View, viewAsm.ModelId);
                            string asmName = $"{viewModelNode.AppNode.Model.Name}.{viewModelNode.Model.Name}";
                            package.ViewAssemblies.Add(asmName, viewAsm.CodeData);
                        }   
                        break;
                    default:
                        Log.Warn($"Unknow pending change: {change.GetType()}"); break;
                }
            }

            PublishService.ValidateModels(hub, package);
            await PublishService.CompileModelsAsync(hub, package);
            await PublishService.PublishAsync(hub, package, commitMessage);

            //最后清空临时用的PendingChanges
            hub.PendingChanges = null;

            return null;
        }
    }
}
