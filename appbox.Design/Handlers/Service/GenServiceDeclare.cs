using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 用于生成服务模型的前端TypeScript调用声明
    /// </summary>
    sealed class GenServiceDeclare : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string modelId = args.GetString();
            ModelNode[] serviceNodes;
            if (string.IsNullOrEmpty(modelId)) //空表示所有服务模型用于初次加载
            {
                serviceNodes = hub.DesignTree.FindNodesByType(ModelType.Service);
            }
            else //指定标识用于更新
            {
                ulong id = ulong.Parse(modelId);
                var node = hub.DesignTree.FindModelNode(ModelType.Service, id);
                serviceNodes = new ModelNode[] { node };
            }

            List<ServiceDeclare> list = new List<ServiceDeclare>();
            foreach (var node in serviceNodes)
            {
                //获取RoslyDocument
                var appName = node.AppNode.Model.Name;
                var doc = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(node.RoslynDocumentId);
                var semanticModel = await doc.GetSemanticModelAsync();
                //TODO: 检测虚拟代码错误
                var codegen = new ServiceDeclareGenerator(hub, appName, semanticModel, (ServiceModel)node.Model);
                codegen.Visit(semanticModel.SyntaxTree.GetRoot());
                list.Add(new ServiceDeclare { Name = $"{appName}.Services.{node.Model.Name}", Declare = codegen.GetDeclare() });
            }

            if (string.IsNullOrEmpty(modelId)) //初次加载时添加系统服务声明
            {
                var adminServiceDeclare = "declare namespace sys.Services.AdminService {function LoadPermissionNodes():Promise<object[]>;function SavePermission(id:string, orgunits:string[]):Promise<void>;}";
                list.Add(new ServiceDeclare { Name = "sys.Services.AdminService", Declare = adminServiceDeclare });
            }

            return list;
        }
    }

    struct ServiceDeclare : IJsonSerializable
    {
        public string Name;
        public string Declare;

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString(nameof(Name), Name);
            writer.WriteString(nameof(Declare), Declare);
        }
    }
}
