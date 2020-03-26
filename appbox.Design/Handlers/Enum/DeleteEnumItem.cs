using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class DeleteEnumItem : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            string modelId = args.GetString();
            string itemName = args.GetString();

            var modelNode = hub.DesignTree.FindModelNode(ModelType.Enum, ulong.Parse(modelId));
            if (modelNode == null)
                throw new Exception("Can't find Enum node");
            var model = (EnumModel)modelNode.Model;

            //查找成员引用
            var refs = await RefactoringService.FindUsagesAsync(hub,
                       ModelReferenceType.EnumModelItemName, modelNode.AppNode.Model.Name, model.Name, itemName);
            if (refs != null && refs.Count > 0) //有引用项不做删除操作
                return refs; //TODO:直接报错，不返回引用项

            var item = model.Items.FirstOrDefault(t => t.Name == itemName);
            if (item != null)
                model.Items.Remove(item);
            // 保存到本地
            await modelNode.SaveAsync(null);
            // 更新RoslynDocument
            await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);

            return null;
        }
    }
}
