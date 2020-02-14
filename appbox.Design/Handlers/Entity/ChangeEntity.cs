using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class ChangeEntity : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelId = args.GetString();
            var changeType = args.GetString();

            var node = hub.DesignTree.FindNode(DesignNodeType.EntityModelNode, modelId);
            if (node == null)
                throw new Exception("Can't find entity model node");
            var modelNode = node as ModelNode;
            var model = modelNode.Model as EntityModel;
            if (!modelNode.IsCheckoutByMe)
                throw new Exception("Node has not checkout");

            //注意某些操作需要更新RoslynDocument
            switch (changeType)
            {
                case "PrimaryKeys":
                    if (model.PersistentState != PersistentState.Detached)
                    {
                        //TODO:如果是修改则必须查找服务方法内的引用，签出节点并修改
                        //1. new XXXX(pks)改为new XXX(/*fix pk changed*/)
                        //2. Entities.XXX.LoadAsync(pks)同上
                        throw new NotImplementedException("改变主键尚未实现");
                    }
                    ChangePrimaryKeys(model, args.GetString());
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    //TODO:同时返回签出列表
                    break;
                case "PartitionKeys":
                    ChangePartitionKeys(model, args.GetString());
                    if (model.SysStoreOptions != null) //仅SysStore需要更新RoslynDocument
                        await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    break;
                case "ClusteringColumns":
                    ChangeClusteringColumns(model, args.GetString());
                    break;
                case "AddIndex":
                    var res = AddIndex(model, args.GetString());
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    return res;
                case "RemoveIndex":
                    await RemoveIndex(hub, modelNode.AppNode.Model.Name, model, args.GetByte());
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    break;
                default:
                    throw new NotSupportedException(changeType);
            }

            return null;
        }

        /// <summary>
        /// 仅SqlStore
        /// </summary>
        private static void ChangePrimaryKeys(EntityModel entityModel, string value)
        {
            if (entityModel.SqlStoreOptions == null)
                throw new NotSupportedException("Change PrimaryKeys for none sqlstore entity.");

            var array = JArray.Parse(value);
            if (array.Count == 0)
            {
                entityModel.SqlStoreOptions.SetPrimaryKeys(entityModel, null);
            }
            else
            {
                var newvalue = new List<FieldWithOrder>(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    //注意如果选择的是EntityRef，则加入所有外键成员作为主键
                    var memberId = (ushort)array[i]["MemberId"];
                    var memberModel = entityModel.GetMember(memberId, true);
                    if (memberModel.Type == EntityMemberType.DataField)
                    {
                        newvalue.Add(new FieldWithOrder
                        {
                            MemberId = memberId,
                            OrderByDesc = (bool)array[i]["OrderByDesc"]
                        });
                    }
                    else if (memberModel.Type == EntityMemberType.EntityRef)
                    {
                        var refMemberModel = (EntityRefModel)memberModel;
                        foreach (var fk in refMemberModel.FKMemberIds)
                        {
                            newvalue.Add(new FieldWithOrder
                            {
                                MemberId = fk,
                                OrderByDesc = (bool)array[i]["OrderByDesc"] //TODO:是否应跟引用目标实体主键一致或相反
                            });
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Member[{memberModel.Name}] with type:[{memberModel.Type}] can't be primary key");
                    }

                }
                entityModel.SqlStoreOptions.SetPrimaryKeys(entityModel, newvalue);
            }
        }

        /// <summary>
        /// SysStore or CqlStore
        /// </summary>
        private static void ChangePartitionKeys(EntityModel entityModel, string value)
        {
            if (entityModel.SysStoreOptions != null)
            {
                var array = JArray.Parse(value);
                if (array.Count == 0)
                {
                    entityModel.SysStoreOptions.SetPartitionKeys(entityModel, null);
                }
                else
                {
                    var newvalue = new PartitionKey[array.Count];
                    for (int i = 0; i < array.Count; i++)
                    {
                        newvalue[i] = new PartitionKey()
                        {
                            MemberId = (ushort)array[i]["MemberId"],
                            Rule = (PartitionKeyRule)((int)array[i]["Rule"]),
                            RuleArgument = (int)array[i]["RuleArg"],
                            OrderByDesc = (bool)array[i]["OrderByDesc"]
                        };
                    }
                    entityModel.SysStoreOptions.SetPartitionKeys(entityModel, newvalue);
                }
            }
            else if (entityModel.CqlStoreOptions != null)
            {
                if (entityModel.PersistentState != PersistentState.Detached)
                    throw new NotSupportedException("Can't change pk for none new EntityModel");

                var pks = System.Text.Json.JsonSerializer.Deserialize<ushort[]>(value);
                entityModel.CqlStoreOptions.PrimaryKey.PartitionKeys = pks;
                OnCqlPKChanged(entityModel); //改变主键成员的AllowNull
            }
            else
            {
                throw new NotSupportedException("Change PartitionKeys for none SysStore nor CqlStore entity.");
            }
        }

        /// <summary>
        /// 仅CqlStore
        /// </summary>
        private static void ChangeClusteringColumns(EntityModel entityModel, string value)
        {
            if (entityModel.PersistentState != PersistentState.Detached)
                throw new NotSupportedException("Can't change pk for none new EntityModel");

            var array = JArray.Parse(value);
            if (array.Count == 0)
            {
                entityModel.CqlStoreOptions.PrimaryKey.ClusteringColumns = null;
            }
            else
            {
                var newvalue = new FieldWithOrder[array.Count];
                for (int i = 0; i < array.Count; i++)
                {
                    newvalue[i] = new FieldWithOrder()
                    {
                        MemberId = (ushort)array[i]["MemberId"],
                        OrderByDesc = (bool)array[i]["OrderByDesc"]
                    };
                }
                entityModel.CqlStoreOptions.PrimaryKey.ClusteringColumns = newvalue;
            }
            OnCqlPKChanged(entityModel);//改变主键成员的AllowNull
        }

        /// <summary>
        /// CqlStore的实体的主键改变后设置相应的AllowNull属性
        /// </summary>
        private static void OnCqlPKChanged(EntityModel model)
        {
            foreach (var member in model.Members)
            {
                if (model.CqlStoreOptions.PrimaryKey.IsPrimaryKey(member.MemberId))
                    member.AllowNull = false;
                else
                    member.AllowNull = true;
            }
        }

        /// <summary>
        /// SysStore及SqlStore通用
        /// </summary>
        private static IndexModelBase AddIndex(EntityModel entityModel, string value)
        {
            if (entityModel.StoreOptions == null)
                throw new InvalidOperationException("Can't add index for DTO");

            var indexInfo = JsonConvert.DeserializeObject<IndexInfo>(value);
            //Validate
            if (string.IsNullOrEmpty(indexInfo.Name)) throw new Exception("Index has no name");
            if (!CodeHelper.IsValidIdentifier(indexInfo.Name)) throw new Exception("Index name not valid");
            if (indexInfo.Fields == null || indexInfo.Fields.Length == 0) throw new Exception("Index has no fields");
            if (entityModel.StoreOptions.HasIndexes
                && entityModel.StoreOptions.Indexes.Any(t => t.Name == indexInfo.Name))
                throw new Exception("Index name has existed");

            var fields = new FieldWithOrder[indexInfo.Fields.Length];
            for (int i = 0; i < indexInfo.Fields.Length; i++)
            {
                fields[i] = new FieldWithOrder(indexInfo.Fields[i].MID, indexInfo.Fields[i].OrderByDesc);
            }

            //根据存储类型新建索引并添加至模型内
            if (entityModel.SysStoreOptions != null)
            {
                var newIndex = new EntityIndexModel(entityModel, indexInfo.Name, indexInfo.Unique, fields, null);
                entityModel.SysStoreOptions.AddIndex(entityModel, newIndex);
                return newIndex;
            }
            else
            {
                var newIndex = new SqlIndexModel(entityModel, indexInfo.Name, indexInfo.Unique, fields, null);
                entityModel.SqlStoreOptions.AddIndex(entityModel, newIndex);
                return newIndex;
            }
        }

        /// <summary>
        /// SysStore及SqlStore通用
        /// </summary>
        private static async Task RemoveIndex(DesignHub hub, string appName, EntityModel entityModel, byte indexId)
        {
            if (entityModel.StoreOptions == null)
                throw new InvalidOperationException("Can't remove index for DTO");
            if (!entityModel.StoreOptions.HasIndexes) throw new Exception($"EntityModel[{entityModel.Name}] not has any indexes.");

            var index = entityModel.StoreOptions.Indexes.SingleOrDefault(t => t.IndexId == indexId);
            if (index == null) throw new Exception($"EntityModel[{entityModel.Name}] has no index: {indexId}");

            if (entityModel.SysStoreOptions != null)
            {
                //查询服务代码引用项
                var refs = await RefactoringService.FindUsagesAsync(hub,
                           ModelReferenceType.EntityIndexName, appName, entityModel.Name, index.Name);
                if (refs != null && refs.Count > 0)
                    throw new Exception($"EntityIndex[{entityModel.Name}.{index.Name}] has references.");
            }

            index.MarkDeleted();
        }

        #region ====前端参数的Json映射结构====
        struct IndexInfo
        {
            public string Name { get; set; }
            public bool Unique { get; set; }
            public IndexFieldInfo[] Fields { get; set; }
        }

        struct IndexFieldInfo
        {
            public ushort MID { get; set; }
            public string Name { get; set; }
            public bool OrderByDesc { get; set; }
        }
        #endregion
    }
}
