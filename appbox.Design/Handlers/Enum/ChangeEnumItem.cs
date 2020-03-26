using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 修改枚举成员属性
    /// </summary>
    sealed class ChangeEnumItem : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var memeberName = args.GetString();
            var propertyName = args.GetString();

            var modelNode = hub.DesignTree.FindModelNode(ModelType.Enum, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception("Can't find Enum node");
            var model = (EnumModel)modelNode.Model;

            var item = model.Items.FirstOrDefault(t => t.Name == memeberName);
            if (item == null)
                throw new Exception($"Can't find Enum item: {memeberName}");
            if (propertyName == "Comment")
            {
                var propertyValue = args.GetString();
                item.Comment = propertyValue;
            }
            else if (propertyName == "Value")
            {
                var v = args.GetInt32();
                if (model.Items.FirstOrDefault(t => t.Value == v && t.Name != memeberName) != null)
                    throw new Exception("Value has exists");
                item.Value = v;
                //TODO:***签出所有此成员的引用项，如服务模型需要重新编译发布
            }
            return Task.FromResult<object>(true);
        }
    }
}
