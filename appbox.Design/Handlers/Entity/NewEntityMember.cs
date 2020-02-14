using System;
using System.Linq;
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
                    res = NewDataField(model, memberName, ref args);
                    break;
                case (int)EntityMemberType.EntityRef:
                    res = NewEntityRef(hub, model, memberName, ref args);
                    break;
                case (int)EntityMemberType.EntitySet:
                    res = NewEntitySet(hub, model, memberName, ref args);
                    break;
                default:
                    throw new NotImplementedException($"未实现的成员类型: {entityMemberType}");
            }

            // 保存到本地
            await modelNode.SaveAsync(null);
            // 更新RoslynDocument
            await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);

            return res;
        }

        private EntityMemberModel NewDataField(EntityModel model, string name, ref InvokeArgs args)
        {
            int fieldType = args.GetInt32();
            bool allowNull = args.GetBoolean();

            var df = new DataFieldModel(model, name, (EntityFieldType)fieldType);
            df.AllowNull = allowNull;
            model.AddMember(df);
            if (!allowNull) //注意：必须在model.AddMember之后，否则mid为0
            {
                var defautString = args.GetString();
                if (!string.IsNullOrEmpty(defautString))
                {
                    try
                    {
                        df.SetDefaultValue(defautString);
                    }
                    catch (Exception ex) //不向前端抛错误，只警告
                    {
                        Log.Warn($"Set default value error: {ex.Message}");
                    }
                }
            }

            return df;
        }

        private EntityMemberModel NewEntityRef(DesignHub hub, EntityModel model, string name, ref InvokeArgs args)
        {
            bool allowNull = args.GetBoolean();
            string refIdStr = args.GetString();
            var isReverse = args.GetBoolean(); // EntityRef 标记是否是反向引用
            if (string.IsNullOrWhiteSpace(refIdStr))
                throw new ArgumentException("请选择对应的引用模型");

            // 解析并检查所有引用类型的正确性
            var refIds = refIdStr.Split(',');
            var refModels = new EntityModel[refIds.Length];
            for (int i = 0; i < refIds.Length; i++)
            {
                if (!(hub.DesignTree.FindNode(DesignNodeType.EntityModelNode, refIds[i]) is ModelNode refModelNode))
                    throw new Exception($"引用的实体模型[{refIds[i]}]不存在");
                var refModel = (EntityModel)refModelNode.Model;
                if (model.StoreOptions != null && refModel.StoreOptions != null)
                {
                    if (model.StoreOptions.GetType() != refModel.StoreOptions.GetType())
                        throw new Exception("Can't reference to different store");
                    if (model.SqlStoreOptions != null
                        && model.SqlStoreOptions.StoreModelId != refModel.SqlStoreOptions.StoreModelId)
                        throw new Exception("Can't reference to different store");
                    if (model.SqlStoreOptions != null && !refModel.SqlStoreOptions.HasPrimaryKeys)
                        throw new Exception("Can't reference to entity without primary key");
                }
                else
                {
                    if (model.StoreOptions == null || refModel.StoreOptions == null)
                        throw new Exception("Can't reference to different store");
                }
                refModels[i] = refModel;
                //检查所有主键的数量、类型是否一致
                if (model.SqlStoreOptions != null && i > 0)
                {
                    if (refModel.SqlStoreOptions.PrimaryKeys.Count != refModels[0].SqlStoreOptions.PrimaryKeys.Count)
                        throw new Exception("聚合引用目标的主键数量不一致");
                    //TODO:判断主键类型是否一致
                    throw new NotImplementedException();
                    //for (int j = 0; j < refModel.SqlStoreOptions.PrimaryKeys.Count; j++)
                    //{
                    //}
                }
            }

            //检查外键字段名称是否已存在，并且添加外键成员
            if (refIds.Length > 1 && model.Members.FindIndex(t => t.Name == $"{name}Type") >= 0)
                throw new Exception($"Name has exists: {name}Type");
            var fkMemberIds = new ushort[refModels.Length];
            if (model.SqlStoreOptions != null)
            {
                //聚合引用以第一个的主键作为外键的名称
                for (int i = 0; i < refModels[0].SqlStoreOptions.PrimaryKeys.Count; i++)
                {
                    var pk = refModels[0].SqlStoreOptions.PrimaryKeys[0];
                    var pkMemberModel = (DataFieldModel)refModels[0].GetMember(pk.MemberId, true);
                    var fkName = $"{name}{pkMemberModel.Name}";
                    if (model.Members.FindIndex(t => t.Name == fkName) >= 0)
                        throw new Exception($"Name has exists: {fkName}");
                    var fk = new DataFieldModel(model, fkName, pkMemberModel.DataType, true);
                    fk.AllowNull = allowNull;
                    model.AddMember(fk);
                    fkMemberIds[i] = fk.MemberId;
                }
            }
            else
            {
                if (model.Members.FindIndex(t => t.Name == $"{name}Id") >= 0)
                    throw new Exception($"Name has exists: {name}Id");
                // 添加外键Id列, eg: Customer -> CustomerId
                var fkId = new DataFieldModel(model, $"{name}Id", EntityFieldType.EntityId, true);
                fkId.AllowNull = allowNull;
                model.AddMember(fkId);
                fkMemberIds[0] = fkId.MemberId;
            }
            // 如果为聚合引用则添加对应的Type列, eg: CostBill -> CostBillType
            EntityRefModel erf;
            if (refIds.Length > 1)
            {
                var fkType = new DataFieldModel(model, $"{name}Type", EntityFieldType.UInt64, true);
                fkType.AllowNull = allowNull;
                model.AddMember(fkType);
                erf = new EntityRefModel(model, name, refIds.Cast<ulong>().ToList(), fkMemberIds, fkType.MemberId);
            }
            else
            {
                erf = new EntityRefModel(model, name, ulong.Parse(refIds[0]), fkMemberIds); //TODO:入参指明是否外键约束
            }
            erf.AllowNull = allowNull;
            model.AddMember(erf);
            return erf;
        }

        private EntityMemberModel NewEntitySet(DesignHub hub, EntityModel model, string name, ref InvokeArgs args)
        {
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

            var esm = new EntitySetModel(model, name, refModelId, refMemberId);
            model.AddMember(esm);
            return esm;
        }
    }
}
