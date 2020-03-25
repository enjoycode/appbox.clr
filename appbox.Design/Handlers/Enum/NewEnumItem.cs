using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewEnumItem : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string modelId = args.GetString();
            string itemName = args.GetString();
            int value = args.GetInt32();
            string comment = args.GetString();

            var node = hub.DesignTree.FindModelNode(ModelType.Enum, ulong.Parse(modelId));
            if (node == null)
                throw new Exception("Can't find enum model node");
            var model = (EnumModel)node.Model;
            if (!node.IsCheckoutByMe)
                throw new Exception("Node has not checkout");
            if (!CodeHelper.IsValidIdentifier(itemName))
                throw new Exception("Name is invalid");
            if (itemName == model.Name)
                throw new Exception("Name can not same as Enum name");
            if (model.Items.FirstOrDefault(t => t.Name == itemName) != null)
                throw new Exception("Name has exists");
            if (model.Items.FirstOrDefault(t => t.Value == value) != null)
                throw new Exception("Value has exists");

            var item = new EnumModelItem(itemName, value);
            if (!string.IsNullOrEmpty(comment))
                item.Comment = comment;
            model.Items.Add(item);

            // 保存到本地
            await node.SaveAsync(null);
            // 更新RoslynDocument
            await hub.TypeSystem.UpdateModelDocumentAsync(node);

            return item;
        }
    }
}
