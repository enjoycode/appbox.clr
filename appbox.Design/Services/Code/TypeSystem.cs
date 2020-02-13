using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OmniSharp;
using appbox.Models;
using System.Collections.Immutable;
using System.Reflection;

namespace appbox.Design
{

    /// <summary>
    /// 每个开发人员会话的设计时上下文对应一个TypeSystem实例
    /// </summary>
    sealed class TypeSystem //TODO: rename to VirtualSolution or Workspace?
    {

        #region ====Properties====
        internal readonly OmniSharpWorkspace Workspace;

        /// <summary>
        /// 通用模型的虚拟代码项目标识号
        /// </summary>
        internal readonly ProjectId ModelProjectId;

        /// <summary>
        /// 服务模型基础项目，具备BaseSyncServiceDummyCode.cs
        /// </summary>
        internal readonly ProjectId ServiceBaseProjectId;

        /// <summary>
        /// 异步服务代理项目标识号，用于UI的EventAction引用
        /// </summary>
        internal readonly ProjectId AsyncServiceProxyProjectId;

        ///// <summary>
        ///// 常规表达式的项目标识，引用SyncServiceProxyProject
        ///// </summary>
        //internal readonly ProjectId ExpressionProjectId;

        /// <summary>
        /// 工作流专用项目，由服务项目及工作流相关Activity的表达式项目引用
        /// </summary>
        internal readonly ProjectId WorkflowModelProjectId;

        /// <summary>
        /// 虚拟代码基类文档Id
        /// </summary>
        internal readonly DocumentId BaseDummyCodeDocumentId;

        internal readonly DocumentId BaseWFDummyCodeDocumentId;

        internal readonly DocumentId ServiceBaseDummyCodeDocumentId;
        #endregion

        #region ====Ctor====
        public TypeSystem()
        {
            //Create Workspace
            Workspace = new OmniSharpWorkspace(new HostServicesAggregator(/*null*/));

            //虚拟项目
            ModelProjectId = ProjectId.CreateNewId();
            WorkflowModelProjectId = ProjectId.CreateNewId();
            ServiceBaseProjectId = ProjectId.CreateNewId();
            AsyncServiceProxyProjectId = ProjectId.CreateNewId();
            //ExpressionProjectId = ProjectId.CreateNewId();

            //各基本虚拟代码
            BaseDummyCodeDocumentId = DocumentId.CreateNewId(ModelProjectId);
            ServiceBaseDummyCodeDocumentId = DocumentId.CreateNewId(ServiceBaseProjectId);
            BaseWFDummyCodeDocumentId = DocumentId.CreateNewId(WorkflowModelProjectId);

            var dllCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var modelProjectInfo = ProjectInfo.Create(ModelProjectId, VersionStamp.Create(),
                                                      "ModelProject", "ModelProject", LanguageNames.CSharp, null, null,
                                                      dllCompilationOptions);
            var workflowProjectInfo = ProjectInfo.Create(WorkflowModelProjectId, VersionStamp.Create(),
                                                      "WorkflowProject", "WorkflowProject", LanguageNames.CSharp, null, null,
                                                      dllCompilationOptions);
            var serviceBaseProjectInfo = ProjectInfo.Create(ServiceBaseProjectId, VersionStamp.Create(),
                                                      "ServiceBaseProject", "ServiceBaseProject", LanguageNames.CSharp, null, null,
                                                      dllCompilationOptions);
            var asyncServiceProjectInfo = ProjectInfo.Create(AsyncServiceProxyProjectId, VersionStamp.Create(),
                                                      "AsyncServiceProxyProject", "AsyncServiceProxyProject", LanguageNames.CSharp, null, null,
                                                      dllCompilationOptions);
            //var expressionProjectInfo = ProjectInfo.Create(ExpressionProjectId, VersionStamp.Create(),
            //                                          "ExpressionProject", "ExpressionProject", LanguageNames.CSharp, null, null,
            //                                          dllCompilationOptions);

            var newSolution = Workspace.CurrentSolution
                      .AddProject(modelProjectInfo)
                      .AddMetadataReference(ModelProjectId, MetadataReferences.CoreLib)
                      .AddMetadataReference(ModelProjectId, MetadataReferences.NetstandardLib)
                      .AddDocument(BaseDummyCodeDocumentId, "BaseDummyCode.cs", CodeGenService.GenBaseDummyCode())

                      .AddProject(asyncServiceProjectInfo)
                      .AddMetadataReference(AsyncServiceProxyProjectId, MetadataReferences.CoreLib)
                      .AddMetadataReference(AsyncServiceProxyProjectId, MetadataReferences.NetstandardLib)
                      .AddProjectReference(AsyncServiceProxyProjectId, new ProjectReference(ModelProjectId))

                      .AddProject(serviceBaseProjectInfo)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.CoreLib)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.NetstandardLib)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.SystemRuntimLib)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.TasksLib)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.TasksExtLib)
                      .AddMetadataReference(ServiceBaseProjectId, MetadataReferences.DataCommonLib)
                      .AddProjectReference(ServiceBaseProjectId, new ProjectReference(ModelProjectId))
                      .AddProjectReference(ServiceBaseProjectId, new ProjectReference(AsyncServiceProxyProjectId))
                      .AddDocument(ServiceBaseDummyCodeDocumentId, "ServiceBaseDummyCode.cs", CodeGenService.GenServiceBaseDummyCode())

                      .AddProject(workflowProjectInfo)
                      .AddMetadataReference(WorkflowModelProjectId, MetadataReferences.CoreLib)
                      .AddMetadataReference(WorkflowModelProjectId, MetadataReferences.NetstandardLib)
                      .AddProjectReference(WorkflowModelProjectId, new ProjectReference(ModelProjectId))
                      .AddDocument(BaseWFDummyCodeDocumentId, "BaseWFDummyCode.cs", CodeGenService.GenBaseWFDummyCode())
           ;
            //.AddProject(expressionProjectInfo)
            //.AddMetadataReference(ExpressionProjectId, MetadataReferences.CoreLib)
            //.AddMetadataReference(ExpressionProjectId, MetadataReferences.NetstandardLib)
            ////.AddMetadataReference(ExpressionProjectId, GetMetadataReference("System.dll"))
            //.AddProjectReference(ExpressionProjectId, new ProjectReference(ModelProjectId))
            //.AddProjectReference(ExpressionProjectId, new ProjectReference(SyncServiceProxyProjectId));

            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn("Cannot create default workspace.");
        }
        #endregion

        #region ====Model & Expression Document Methods====
        /// <summary>
        /// 创建服务模型的虚拟项目，即一个服务模型对应一个虚拟项目
        /// </summary>
        internal ProjectId CreateServiceProject(ProjectId prjid, ServiceModel model, string appName)
        {
            var prjName = $"{appName}.{model.Name}"; //TODO: 使用Id作为名称
            var dllCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var serviceProjectInfo = ProjectInfo.Create(prjid, VersionStamp.Create(),
                                                        prjName, prjName, LanguageNames.CSharp, null, null,
                                                        dllCompilationOptions);

            var deps = new List<MetadataReference>
            {
                MetadataReferences.CoreLib,
                MetadataReferences.NetstandardLib,
                //MetadataReferences.SystemCoreLib,
                MetadataReferences.SystemLinqLib,
                MetadataReferences.SystemRuntimLib,
                MetadataReferences.SystemRuntimExtLib,
                MetadataReferences.DataCommonLib,
                MetadataReferences.ComponentModelPrimitivesLib,
                //MetadataReferences.SystemBuffersLib,
                MetadataReferences.TasksLib,
                MetadataReferences.TasksExtLib
            };
            if (model.HasReference) //添加其他引用
            {
                for (int i = 0; i < model.References.Count; i++)
                {
                    deps.Add(MetadataReferences.Get($"{model.References[i]}.dll", appName));
                }
            }

            var newSolution = Workspace.CurrentSolution
                .AddProject(serviceProjectInfo)
                .AddMetadataReferences(prjid, deps)
                .AddProjectReference(prjid, new ProjectReference(ModelProjectId))
                .AddProjectReference(prjid, new ProjectReference(ServiceBaseProjectId));
            //.AddProjectReference(prjid, new ProjectReference(WorkflowModelProjectId));

            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn("Cannot create service project.");

            return prjid;
        }

        internal void RemoveServiceProject(ProjectId serviceProjectId)
        {
            var newSolution = Workspace.CurrentSolution.RemoveProject(serviceProjectId);
            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn("Cannot remove service project.");
        }

        internal void AddServiceReference(ProjectId serviceProjectId, string appID, string reference)
        {
            var dep = MetadataReferences.Get($"{reference}.dll", appID);
            var newSolution = Workspace.CurrentSolution.AddMetadataReference(serviceProjectId, dep);
            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn("Cannot remove service project reference.");
        }

        internal void RemoveServiceReference(ProjectId serviceProjectId, string appID, string reference)
        {
            // var project = Workspace.CurrentSolution.GetProject(serviceProjectId);
            // var mrf = project.MetadataReferences.FirstOrDefault(t => t.Display == reference);
            var dep = MetadataReferences.Get($"{reference}.dll", appID);
            var newSolution = Workspace.CurrentSolution.RemoveMetadataReference(serviceProjectId, dep);
            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn("Cannot remove service project reference.");
        }

        /// <summary>
        /// Creates DataStore roslyn document
        /// </summary>
        internal void CreateStoreDocument(DataStoreNode node)
        {
            var docName = $"sys.DataStore.{node.Model.Name}.cs";
            var newSolution = Workspace.CurrentSolution.AddDocument(node.RoslynDocumentId, docName,
                            CodeGenService.GenDataStoreDummyCode(node.Model));

            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn($"Cannot add roslyn document for: {node.Model.Name}");
        }

        /// <summary>
        /// Creates the model's roslyn document
        /// </summary>
        internal async ValueTask CreateModelDocumentAsync(ModelNode node, string initServiceCode = null)
        {
            //TODO: fix others, 另考虑代码统一由调用者传入
            Solution newSolution = null;
            var appName = node.AppNode.Model.Name;
            var model = node.Model;
            var docId = node.RoslynDocumentId;

            switch (model.ModelType)
            {
                case ModelType.Entity:
                    {
                        var docName = $"{appName}.Entities.{model.Name}.cs";
                        var dummyCode = CodeGenService.GenEntityDummyCode((EntityModel)model, appName, node.DesignTree);
                        newSolution = Workspace.CurrentSolution.AddDocument(docId, docName, dummyCode);
                    }
                    break;
                //case ModelType.Enum:
                //    {
                //        var docName = string.Format("{0}.Enums.{1}.cs", model.AppID, model.Name);
                //        newSolution = Workspace.CurrentSolution.AddDocument(docId, docName,
                //            CodeGenService.GenEnumDummyCode((EnumModel)model));
                //    }
                //    break;
                case ModelType.Service:
                    {
                        //注意: 服务模型先创建虚拟项目
                        CreateServiceProject(node.ServiceProjectId, (ServiceModel)model, appName);

                        var docName = $"{appName}.Services.{model.Name}.cs";
                        string sourceCode = initServiceCode;
                        if (string.IsNullOrEmpty(sourceCode))
                        {
                            if (node.IsCheckoutByMe) //已签出尝试从Staged中加载
                            {
                                sourceCode = await StagedService.LoadServiceCode(model.Id);
                            }
                            if (string.IsNullOrEmpty(sourceCode)) //从ModelStore加载
                            {
                                sourceCode = await Store.ModelStore.LoadServiceCodeAsync(model.Id);
                            }
                        }
                        newSolution = Workspace.CurrentSolution.AddDocument(docId, docName, sourceCode);

                        //服务模型创建代理
                        var proxyDocName = $"{appName}.{model.Name}.AsnycProxy.cs";
                        var srcdoc = newSolution.GetDocument(docId);
                        var proxyCode = await CodeGenService.GenProxyCode(srcdoc, appName, (ServiceModel)model);
                        newSolution = newSolution.AddDocument(node.AsyncProxyDocumentId, proxyDocName, proxyCode);
                    }
                    break;
                case ModelType.Permission:
                    {
                        var docName = $"{appName}.Permissions.{model.Name}.cs";
                        newSolution = Workspace.CurrentSolution.AddDocument(docId, docName,
                            CodeGenService.GenPermissionDummyCode((PermissionModel)model, appName));
                    }
                    break;
                    //case ModelType.Workflow:
                    //{
                    //    var docName = string.Format("{0}.Workflows.{1}.cs", model.AppID, model.Name);
                    //    newSolution = Workspace.CurrentSolution.AddDocument(docId, docName,
                    //        CodeGenService.GenWorkflowDummyCode((WorkflowModel)model));
                    //}
                    //break;
            }

            if (newSolution != null)
            {
                if (!Workspace.TryApplyChanges(newSolution))
                    Log.Warn($"Cannot add roslyn document for: {model.Name}");
            }
        }

        /// <summary>
        /// 更新模型RoslynDocument，注意：服务模型也会更新，如不需要由调用者忽略
        /// </summary>
        internal async ValueTask UpdateModelDocumentAsync(ModelNode node)
        {
            if (node.RoslynDocumentId == null)
                return;

            var appName = node.AppNode.Model.Name;
            var model = node.Model;
            var docId = node.RoslynDocumentId;

            Solution newSolution = null;
            //TODO: others
            switch (model.ModelType)
            {
                case ModelType.Entity:
                    {
                        var sourceCode = CodeGenService.GenEntityDummyCode((EntityModel)model, appName, node.DesignTree);
                        newSolution = Workspace.CurrentSolution.WithDocumentText(docId, SourceText.From(sourceCode));
                    }
                    break;
                //case ModelType.Enum:
                //newSolution = Workspace.CurrentSolution.WithDocumentText(modelDocId,
                //                    SourceText.From(CodeGenService.GenEnumDummyCode((EnumModel)model)));
                //break;
                case ModelType.Service:
                    {
                        var sourceCode = await Store.ModelStore.LoadServiceCodeAsync(model.Id);
                        newSolution = Workspace.CurrentSolution.WithDocumentText(docId, SourceText.From(sourceCode));

                        // 服务模型还需要更新代理类
                        var srcdoc = newSolution.GetDocument(docId);
                        var proxyCode = await CodeGenService.GenProxyCode(srcdoc, appName, (ServiceModel)model);
                        newSolution = newSolution
                                .WithDocumentText(node.AsyncProxyDocumentId, SourceText.From(proxyCode));
                    }
                    break;
            }

            if (newSolution != null)
            {
                if (!Workspace.TryApplyChanges(newSolution))
                    Log.Warn("Cannot update roslyn document for: " + model.Name);
            }
        }

        /// <summary>
        /// 创建表达式RoslynDocument
        /// </summary>
        internal void CreateExpressionDocument(DocumentId docId, string docName, string sourceText)
        {
            var newSolution = Workspace.CurrentSolution.AddDocument(docId, docName, SourceText.From(sourceText));
            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn($"Cannot add expression roslyn document for: {docName}");
        }

        /// <summary>
        /// 用于删除表达式和删除模型时移除相应的RoslynDocument
        /// </summary>
        /// <remarks>
        /// 注意: 如果处于打开状态，则先关闭再移除
        /// </remarks>
        internal void RemoveDocument(DocumentId docId)
        {
            if (Workspace.IsDocumentOpen(docId))
                Workspace.CloseDocument(docId);

            var newSolution = Workspace.CurrentSolution.RemoveDocument(docId);
            if (!Workspace.TryApplyChanges(newSolution))
                Log.Warn($"Cannot remove expression roslyn document for: {docId}");
        }
        #endregion

        #region ====Get Symbol Methods for Refactoring====
        private Document GetModelDocument(ModelType modelType, string appName, string modelName)
        {
            var docName = $"{appName}.{CodeHelper.GetPluralStringOfModelType(modelType)}.{modelName}.cs";
            return Workspace.CurrentSolution.GetProject(ModelProjectId).Documents.SingleOrDefault(t => t.Name == docName);
        }

        /// <summary>
        /// 根据指定的模型类型及标识号获取相应的虚拟类的类型
        /// </summary>
        internal async Task<INamedTypeSymbol> GetModelSymbolAsync(ModelType modelType, string appName, string modelName)
        {
            var doc = GetModelDocument(modelType, appName, modelName);
            if (doc == null)
                return null;

            var syntaxRootNode = await doc.GetSyntaxRootAsync();
            var semanticModel = await doc.GetSemanticModelAsync();

            if (modelType == ModelType.Enum)
                throw ExceptionHelper.NotImplemented(); //TODO:处理枚举等非ClassDeclaration的类型

            ClassDeclarationSyntax classDeclaration = syntaxRootNode
                .DescendantNodes().OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.ValueText == modelName); //注意：不能用Single, 实体模型还生成同名的资源类，或比较全名称
            return semanticModel.GetDeclaredSymbol(classDeclaration);
        }

        //internal static ISymbol GetModelMemberSymbol(ModelType modelType, string modelID, string memberName)
        //{
        //    var modelSymbol = GetModelSymbol(modelType, modelID);
        //    if (modelSymbol == null)
        //        return null;

        //    return modelSymbol.GetMembers(memberName).FirstOrDefault(); //暂返回第一个
        //}

        //TODO: *** only one IPropertySymbol now?
        internal async Task<IPropertySymbol[]> GetEntityMemberSymbolsAsync(string appName, string modelName, string memberName)
        {
            var doc = GetModelDocument(ModelType.Entity, appName, modelName);
            if (doc == null)
                return null;

            var syntaxRootNode = await doc.GetSyntaxRootAsync();
            var semanticModel = await doc.GetSemanticModelAsync();

            var classDeclarations = syntaxRootNode.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.ValueText == modelName).ToArray();
            var symbols = new IPropertySymbol[1];
            symbols[0] = (IPropertySymbol)semanticModel.GetDeclaredSymbol(classDeclarations[0]).GetMembers(memberName).SingleOrDefault();
            //symbols[1] = (IPropertySymbol)semanticModel.GetDeclaredSymbol(classDeclarations[1]).GetMembers(memberName).SingleOrDefault();
            return symbols;
        }

        internal async Task<INamedTypeSymbol> GetEntityIndexSymbolAsync(string appName, string modelName, string indexName)
        {
            var doc = GetModelDocument(ModelType.Entity, appName, modelName);
            if (doc == null) return null;

            var syntaxRootNode = await doc.GetSyntaxRootAsync();
            var semanticModel = await doc.GetSemanticModelAsync();
            var interfaceDeclaration = syntaxRootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                .Where(c => c.Identifier.ValueText == indexName).SingleOrDefault();
            if (interfaceDeclaration == null) return null;

            return semanticModel.GetDeclaredSymbol(interfaceDeclaration);
        }
        #endregion

        #region ====GetProjectErros for debug====
        /// <summary>
        /// 输出虚拟项目错误信息，仅用于调试
        /// </summary>
        internal void DumpProjectErrors(ProjectId projectId)
        {
            var project = Workspace.CurrentSolution.GetProject(projectId);
            var cu = project.GetCompilationAsync().Result;
            var errors = cu.GetDiagnostics();
            foreach (var err in errors)
            {
                Console.WriteLine("项目[{0}]存在错误: {1}", project.Name, err);
            }
        }
        #endregion
    }

}