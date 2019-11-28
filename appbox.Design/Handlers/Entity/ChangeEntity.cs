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
            var value = args.GetObject();

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
                    ChangePrimaryKeys(model, value);
                    break;
                case "PartitionKeys":
                    ChangePartitionKeys(model, value);
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    break;
                case "AddIndex":
                    var res = AddIndex(model, value);
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    return res;
                case "RemoveIndex":
                    await RemoveIndex(hub, modelNode.AppNode.Model.Name, model, value);
                    await hub.TypeSystem.UpdateModelDocumentAsync(modelNode);
                    break;
                default:
                    throw new NotSupportedException(changeType);
            }

            return null;
        }

        private static void ChangePrimaryKeys(EntityModel entityModel, object value)
        {
            if (entityModel.SqlStoreOptions == null)
                throw new NotSupportedException("Change PrimaryKeys for none sqlstore entity.");

            var array = JArray.Parse((string)value);
            if (array.Count == 0)
            {
                entityModel.SqlStoreOptions.SetPrimaryKeys(entityModel, null);
            }
            else
            {
                var newvalue = new List<SqlField>(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    var key = new SqlField
                    {
                        MemberId = (ushort)array[i]["MemberId"],
                        OrderByDesc = (bool)array[i]["OrderByDesc"]
                    };
                    newvalue.Add(key);
                }
                entityModel.SqlStoreOptions.SetPrimaryKeys(entityModel, newvalue);
            }
        }

        private static void ChangePartitionKeys(EntityModel entityModel, object value)
        {
            if (entityModel.SysStoreOptions == null)
                throw new NotSupportedException("Change PartitionKeys for none sysstore entity.");

            var array = JArray.Parse((string)value);
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

        private static EntityIndexModel AddIndex(EntityModel entityModel, object value)
        {
            var indexInfo = JsonConvert.DeserializeObject<IndexInfo>((string)value);
            //Validate
            if (string.IsNullOrEmpty(indexInfo.Name)) throw new Exception("Index has no name");
            if (!CodeHelper.IsValidIdentifier(indexInfo.Name)) throw new Exception("Index name not valid");
            if (indexInfo.Fields == null || indexInfo.Fields.Length == 0) throw new Exception("Index has no fields");

            var fields = new EntityIndexField[indexInfo.Fields.Length];
            for (int i = 0; i < indexInfo.Fields.Length; i++)
            {
                fields[i] = new EntityIndexField(indexInfo.Fields[i].MID, indexInfo.Fields[i].OrderByDesc);
            }

            //新建索引并添加至模型内
            var newIndex = new EntityIndexModel(entityModel, indexInfo.Name, indexInfo.Unique, fields, null);
            entityModel.SysStoreOptions.AddIndex(entityModel, newIndex);
            return newIndex;
        }

        private static async Task RemoveIndex(DesignHub hub, string appName, EntityModel entityModel, object value)
        {
            if (!entityModel.SysStoreOptions.HasIndexes) throw new Exception($"EntityModel[{entityModel.Name}] not has any indexes.");

            byte indexId = Convert.ToByte(value);
            var index = entityModel.SysStoreOptions.Indexes.SingleOrDefault(t => t.IndexId == indexId);
            if (index == null) throw new Exception($"EntityModel[{entityModel.Name}] has no index: {indexId}");

            //查询服务代码引用项
            var refs = await RefactoringService.FindUsagesAsync(hub,
                       ModelReferenceType.EntityIndexName, appName, entityModel.Name, index.Name);
            if (refs != null && refs.Count > 0)
                throw new Exception($"EntityIndex[{entityModel.Name}.{index.Name}] has references.");

            index.MarkDeleted();
        }

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
    }
}
