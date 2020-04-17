using System;
using System.Threading.Tasks;
using appbox.Data;
using OmniSharp.Mef;

namespace appbox.Design
{

    /// <summary>
    /// 保存当前的模型，注意：某些模型需要传入附加的参数
    /// </summary>
    sealed class SaveModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var nodeType = (DesignNodeType)args.GetInt32();
            var modelID = args.GetString();
            object[] modelInfo = null;
            if (nodeType == DesignNodeType.ViewModelNode)
            {
                modelInfo = new object[4];
                modelInfo[0] = args.GetString();
                modelInfo[1] = args.GetString();
                modelInfo[2] = args.GetString();
                modelInfo[3] = args.GetString();
            }
            else if (nodeType == DesignNodeType.ReportModelNode)
            {
                modelInfo = new object[1];
                modelInfo[0] = args.GetString();
            }

            var node = hub.DesignTree.FindNode(nodeType, modelID);
            if (node == null)
                throw new Exception("Can't find node: " + modelID);

            var modelNode = node as ModelNode;
            if (modelNode == null)
                throw new Exception("Node must be ModelNode ");

            //开始保存
            await modelNode.SaveAsync(modelInfo);
            return null;
        }
    }
}
