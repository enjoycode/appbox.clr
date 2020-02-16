using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;
using OmniSharp.Mef;
using appbox.Caching;

namespace appbox.Design
{
    /// <summary>
    /// 用于生成这体模型的前端TypeScript声明
    /// </summary>
    sealed class GenEntityDeclare : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string modelId = args.GetString();
            ModelNode[] modelNodes;
            if (string.IsNullOrEmpty(modelId)) //空表示所有模型用于初次加载
            {
                modelNodes = hub.DesignTree.FindNodesByType(ModelType.Entity);
            }
            else //指定标识用于更新
            {
                ulong id = ulong.Parse(modelId);
                var node = hub.DesignTree.FindModelNode(ModelType.Entity, id);
                modelNodes = new ModelNode[] { node };
            }

            List<TypeScriptDeclare> list = new List<TypeScriptDeclare>();
            foreach (var node in modelNodes)
            {
                list.Add(new TypeScriptDeclare
                {
                    Name = $"{node.AppNode.Model.Name}.Entities.{node.Model.Name}",
                    Declare = BuildDeclare(node, hub)
                });
            }

            return Task.FromResult<object>(list);
        }

        private static string BuildDeclare(ModelNode node, DesignHub hub)
        {
            var app = node.AppNode.Model.Name;
            var model = (EntityModel)node.Model;
            var sb = StringBuilderCache.Acquire();
            sb.Append($"declare namespace {app}.Entities{{");
            sb.Append($"declare class {model.Name} extends EntityBase{{");
            //注意：前端不关注成员是否readonly，以方便UI绑定如主键值
            foreach (var m in model.Members)
            {
                string type = "any";
                switch (m.Type)
                {
                    case EntityMemberType.DataField:
                        type = GetDataFieldType((DataFieldModel)m);
                        break;
                    case EntityMemberType.EntityRef:
                        {
                            var rm = (EntityRefModel)m;
                            for (int i = 0; i < rm.RefModelIds.Count; i++)
                            {
                                var target = hub.DesignTree.FindModelNode(ModelType.Entity, rm.RefModelIds[i]);
                                var typeName = $"{target.AppNode.Model.Name}.Entities.{target.Model.Name}";
                                if (i == 0)
                                    type = typeName;
                                else
                                    type += $" | {typeName}";
                            }
                        }
                        break;
                    case EntityMemberType.EntitySet:
                        {
                            var sm = (EntitySetModel)m;
                            var target = hub.DesignTree.FindModelNode(ModelType.Entity, sm.RefModelId);
                            type = $"{target.AppNode.Model.Name}.Entities.{target.Model.Name}[]";
                        }
                        break;
                }
                //TODO:处理注释
                sb.Append($"{m.Name}:{type};");
            }
            sb.Append("}}");
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static string GetDataFieldType(DataFieldModel df)
        {
            switch (df.DataType)
            {
                case EntityFieldType.EntityId:
                case EntityFieldType.Guid:
                case EntityFieldType.Binary:
                case EntityFieldType.String:
                    return "string";
                case EntityFieldType.Boolean:
                    return "boolean";
                case EntityFieldType.Byte:
                case EntityFieldType.Decimal:
                case EntityFieldType.Double:
                case EntityFieldType.Enum:
                case EntityFieldType.Float:
                case EntityFieldType.UInt16:
                case EntityFieldType.Int16:
                case EntityFieldType.UInt32:
                case EntityFieldType.Int32:
                case EntityFieldType.UInt64:
                case EntityFieldType.Int64:
                    return "number";
                case EntityFieldType.DateTime:
                    return "Date";
                default:
                    return "any";
            }
        }
    }
}
