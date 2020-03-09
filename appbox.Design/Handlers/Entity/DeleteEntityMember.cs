using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class DeleteEntityMember : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            string modelId = args.GetString();
            string memberName = args.GetString();

            var node = hub.DesignTree.FindNode(DesignNodeType.EntityModelNode, modelId);
            if (node == null)
                throw new Exception("Can't find entity model node");
            var modelNode = node as ModelNode;
            var model = modelNode.Model as EntityModel;
            if (!modelNode.IsCheckoutByMe)
                throw new Exception("Node has not checkout");
            var mm = model.GetMember(memberName, true);
            //判断是否外键及被索引使用,仅DataField
            if (mm.Type == EntityMemberType.DataField)
            {
                var dfm = (DataFieldModel)mm;
                if (dfm.IsForeignKey)
                    throw new Exception("Can't delete a foregn key member");
               
                if (model.StoreOptions != null && model.StoreOptions.HasIndexes)
                {
                    foreach (var index in model.StoreOptions.Indexes)
                    {
                        //排除已标为删除的
                        if (index.PersistentState != PersistentState.Deleted)
                        {
                            if (index.Fields.Any(t => t.MemberId == mm.MemberId))
                                throw new Exception($"Member are used in Index[{index.Name}]");
                            if (index.HasStoringFields)
                            {
                                if (index.StoringFields.Any(t => t == mm.MemberId))
                                    throw new Exception($"Member are used in Index[{index.Name}]");
                            }
                        }
                    }
                }
            }
            
            //查找成员引用
            var refs = await RefactoringService.FindUsagesAsync(hub,
                       ModelReferenceType.EntityMemberName, modelNode.AppNode.Model.Name, model.Name, memberName);
            if (refs != null && refs.Count > 0) //有引用项不做删除操作
                return refs; //TODO:直接报错，不返回引用项

            model.RemoveMember(memberName);

            // 保存到本地
            await modelNode.SaveAsync(null);
            // 更新RoslynDocument
            await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);

            return null;
        }
    }
}
