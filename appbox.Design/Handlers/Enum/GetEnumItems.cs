using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 获取指定枚举模型的Items
    /// </summary>
    sealed class GetEnumItems : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.Enum, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception("Can't find EnumModel.");

            var enumModel = (EnumModel)modelNode.Model;
            return Task.FromResult<object>(enumModel.Items);
        }
    }
}
