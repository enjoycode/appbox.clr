using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 获取当前服务模型的依赖项，顺带返回所在的ApplicationModel所依赖的第三方组件列表
    /// </summary>
    sealed class GetReferences : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception("Can't find service model node");

            var appLibs = await Store.ModelStore.LoadAppAssemblies(modelNode.AppNode.Model.Name);
            var res = new
            {
                AppDeps = appLibs,
                ModelDeps = ((ServiceModel)modelNode.Model).References
            };
            return res;
        }
    }
}
