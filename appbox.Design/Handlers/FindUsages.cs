using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 查找模型或模型成员的引用项
    /// </summary>
    sealed class FindUsages : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var refType = (ModelReferenceType)args.GetInt32();
            var modelIDString = args.GetString();
            var memberName = args.GetString();
            var modelId = ulong.Parse(modelIDString);

            ModelType modelType = IdUtil.GetModelTypeFromModelId(modelId);
            var modelNode = hub.DesignTree.FindModelNode(modelType, modelId);
            if (modelNode == null)
                throw new Exception("Can't find model");

            return await RefactoringService.FindUsagesAsync(hub, refType,
                modelNode.AppNode.Model.Name, modelNode.Model.Name, memberName);
        }
    }
}
