using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewEntityMember : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            string modelId = args.GetString();
            string memberName = args.GetString();
            int entityMemberType = args.GetInt32();

            var node = hub.DesignTree.FindNode(DesignNodeType.EntityModelNode, modelId);
            if (node == null)
                throw new Exception("Can't find entity model node");
            var modelNode = node as ModelNode;
            var model = modelNode.Model as EntityModel;
            if (!modelNode.IsCheckoutByMe)
                throw new Exception("Node has not checkout");
            if (!CodeHelper.IsValidIdentifier(memberName))
                throw new Exception("Name is invalid");
            if (memberName == model.Name)
                throw new Exception("Name can not same as Entity name");
            if (model.Members.FindIndex(t => t.Name == memberName) >= 0) //if (model.ContainsMember(memberName))
                throw new Exception("Name has exists");

            EntityMemberModel res;
            switch (entityMemberType)
            {
                case (int)EntityMemberType.DataField:
                    int fieldType = args.GetInt32();
                    bool allowNull = args.GetBoolean();
                    var df = new DataFieldModel(model, memberName, (EntityFieldType)fieldType);
                    df.AllowNull = allowNull;
                    res = df;
                    model.AddMember(df);
                    if (!allowNull) //注意：必须在model.AddMember之后，否则mid为0
                        df.SetDefaultValue(args.GetString());
                    break;
                case (int)EntityMemberType.EntityRef:
                    string refIdStr = args.GetString();
                    var isReverse = args.GetBoolean(); // EntityRef 标记是否是反向引用
                    if (string.IsNullOrWhiteSpace(refIdStr))
                        throw new ArgumentException("请选择对应的引用模型");

                    // 解析并检查所有引用类型的正确性
                    var refIds = refIdStr.Split(',');
                    if (refIds.Length > 1) throw new Exception("暂不支持聚合引用"); //TODO:remove
                    foreach (var refId in refIds)
                    {
                        var refModel = hub.DesignTree.FindNode(DesignNodeType.EntityModelNode, refId);
                        if (refModel == null)
                            throw new Exception($"引用的实体模型[{refId}]不存在");
                    }

                    //检查相关字段名称是否已存在
                    if (model.Members.FindIndex(t => t.Name == $"{memberName}Id") >= 0)
                        throw new Exception("Name has exists");
                    if (refIds.Length > 1 && model.Members.FindIndex(t => t.Name == $"{memberName}Type") >= 0)
                        throw new Exception("Name has exists");

                    // 添加ID列
                    var erfid = new DataFieldModel(model, $"{memberName}Id", EntityFieldType.EntityId);
                    erfid.AllowNull = true;
                    model.AddMember(erfid);
                    // 如果为聚合引用则添加对应的Type列
                    if (refIds.Length > 1)
                    {
                        throw ExceptionHelper.NotImplemented(); //TODO:
                        //var erftype = new DataFieldModel(model);
                        //erftype.AllowNull = true;
                        //erftype.DataType = EntityFieldType.Guid;
                        //erftype.FieldName = erf.TypeMemberName;
                        //erftype.Name = erf.TypeMemberName;
                        //erftype.IsRefKey = true;
                        //model.AddMember(erftype);
                    }
                    var erf = new EntityRefModel(model, memberName, ulong.Parse(refIds[0]), erfid.MemberId); //TODO:入参指明是否外键约束
                    erf.AllowNull = true;
                    res = erf;
                    model.AddMember(erf);
                    break;
                case (int)EntityMemberType.EntitySet:
                    var refModelId = ulong.Parse(args.GetString());
                    var refMemberId = (ushort)args.GetInt32();
                    //验证引用目标存在
                    var target = hub.DesignTree.FindModelNode(ModelType.Entity, refModelId);
                    if (target == null)
                        throw new Exception("Can't find EntityRef");
                    var targetModel = (EntityModel)target.Model;
                    var targetMember = targetModel.GetMember(refMemberId, true);
                    if (targetMember.Type != EntityMemberType.EntityRef)
                        throw new Exception("RefMember isnot EntityRef");

                    var esm = new EntitySetModel(model, memberName, refModelId, refMemberId);
                    res = esm;
                    model.AddMember(esm);
                    break;
                default:
                    throw new NotImplementedException("未实现的成员类型");
            }

            // 保存到本地
            await modelNode.SaveAsync(null);
            // 更新RoslynDocument
            await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);

            return res;
        }
    }
}
