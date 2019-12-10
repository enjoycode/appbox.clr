using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Store;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using appbox.Runtime;
using System.IO.Compression;
using System.Data.Common;

namespace appbox.Design
{
    static class PublishService
    {

        internal static void ValidateModels(DesignHub hub, PublishPackage package)
        {
            //TODO:
        }

        internal static async Task CompileModelsAsync(DesignHub hub, PublishPackage package)
        {
            foreach (var item in hub.PendingChanges)
            {
                if (item is ServiceModel sm && sm.PersistentState != PersistentState.Deleted)
                {
                    var asmData = await CompileServiceAsync(hub, sm);
                    var appName = hub.DesignTree.FindApplicationNode(sm.AppId).Model.Name;
                    var fullName = $"{appName}.{sm.Name}";
                    //TODO:重命名的需要加入待删除列表
                    package.ServiceAssemblies.Add(fullName, asmData);
                }
            }
        }

        /// <summary>
        /// 发布或调试时编译服务模型
        /// </summary>
        /// <remarks>
        /// 发布时返回的是已经压缩过的
        /// </remarks>
        internal static async Task<byte[]> CompileServiceAsync(DesignHub hub, ServiceModel model, string debugFolder = null)
        {
            bool forDebug = !string.IsNullOrEmpty(debugFolder);
            //获取RoslyDocumentId
            var designNode = hub.DesignTree.FindModelNode(ModelType.Service, model.Id);
            var appName = designNode.AppNode.Model.Name;
            //获取RoslyDocument
            var doc = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(designNode.RoslynDocumentId);
            var semanticModel = await doc.GetSemanticModelAsync();

            //先检测虚拟代码错误
            var diagnostics = semanticModel.GetDiagnostics();
            if (diagnostics.Length > 0)
            {
                bool hasError = false;
                var sb = new StringBuilder("语法错误:");
                sb.AppendLine();
                for (int i = 0; i < diagnostics.Length; i++)
                {
                    var error = diagnostics[i];
                    if (error.WarningLevel == 0)
                    {
                        hasError = true;
                        sb.AppendFormat("{0}. {1} {2}{3}", i + 1, error.WarningLevel, error.GetMessage(), Environment.NewLine);
                    }

                }
                if (hasError)
                    throw new Exception(sb.ToString());
            }

            var codegen = new ServiceCodeGenerator(hub, appName, semanticModel, model);
            var newRootNode = codegen.Visit(semanticModel.SyntaxTree.GetRoot()); //.NormalizeWhitespace();

            var docName = string.Format("{0}.Services.{1}", appName, model.Name);
            var newTree = SyntaxFactory.SyntaxTree(newRootNode, path: docName + ".cs", encoding: Encoding.UTF8);
            //注意：必须添加并更改版本号，否则服务端Assembly.Load始终是旧版 
            var newModelVersion = model.Version + 1; //用于消除版本差
            var versionTree = SyntaxFactory.ParseSyntaxTree("using System.Reflection;using System.Runtime.CompilerServices;using System.Runtime.Versioning;[assembly:TargetFramework(\".NETStandard, Version = v2.0\")][assembly: AssemblyVersion(\""
                                                            + newModelVersion.ToString() + "\")]");
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                false, null, null, null, null, forDebug ? OptimizationLevel.Debug : OptimizationLevel.Release);
            var deps = new List<MetadataReference>
            {
                MetadataReferences.CoreLib,
                MetadataReferences.NetstandardLib,
                MetadataReferences.SystemRuntimLib,
                MetadataReferences.SystemRuntimExtLib,
                MetadataReferences.TasksLib,
                MetadataReferences.TasksExtLib,
                MetadataReferences.DataCommonLib,
                //MetadataReferences.SystemBuffersLib,
                MetadataReferences.AppBoxCoreLib,
                MetadataReferences.AppBoxStoreLib
            };

            if (model.HasReference) //添加其他引用
            {
                for (int i = 0; i < model.References.Count; i++)
                {
                    deps.Add(MetadataReferences.Get($"{model.References[i]}.dll", appName));
                }
            }

            var compilation = CSharpCompilation.Create(docName)
                                               .AddReferences(deps)
                                               .AddSyntaxTrees(newTree, versionTree)
                                               .WithOptions(options);
            EmitResult emitResult = null;
            byte[] asmData = null;
            if (forDebug)
            {
                //TODO:考虑改写入内置文件存储系统
                using var dllStream = new FileStream(Path.Combine(debugFolder, docName + ".dll"), FileMode.CreateNew);
                using var pdbStream = new FileStream(Path.Combine(debugFolder, docName + ".pdb"), FileMode.CreateNew);
                emitResult = compilation.Emit(dllStream, pdbStream);
            }
            else
            {
                using var dllStream = new MemoryStream(1024);
                using (var cs = new BrotliStream(dllStream, CompressionMode.Compress, true))
                {
                    emitResult = compilation.Emit(cs);
                }
                asmData = dllStream.ToArray();
            }

            //测试写入本地文件系统
            //File.WriteAllBytes(Path.Combine(RuntimeContext.Current.AppPath, $"{appName}.{model.Name}.dll"), asmData);

            if (!emitResult.Success)
            {
                var sb = new StringBuilder("编译错误:");
                sb.AppendLine();
                for (int i = 0; i < emitResult.Diagnostics.Length; i++)
                {
                    var error = emitResult.Diagnostics[i];
                    sb.AppendFormat("{0}. {1}", i + 1, error);
                    sb.AppendLine();
                }
                throw new Exception(sb.ToString());
            }

            return forDebug ? null : asmData;
        }

        /// <summary>
        /// 1. 保存模型(包括编译好的服务Assembly)，并生成EntityModel的SchemaChangeJob;
        /// 2. 通知集群各节点更新缓存;
        /// 3. 删除当前会话的CheckoutInfo;
        /// 4. 刷新DesignTree相应的节点，并删除挂起
        /// 5. 保存递交日志
        /// </summary>
        internal static async Task PublishAsync(DesignHub hub, PublishPackage package, string commitMessage)
        {
            package.SortAllModels();

            var txn = await Transaction.BeginAsync();
            //注意目前实现无法保证第三方数据库与内置模型存储的一致性,第三方数据库发生异常只能手动清理
            var otherStoreTxns = new Dictionary<ulong, DbTransaction>();
            //TODO:发布锁
            try
            {
                await SaveModelsAsync(hub, package, txn, otherStoreTxns);

                await CheckoutService.CheckinAsync(txn);

                //注意必须先刷新后清除缓存，否则删除的节点在移除后会自动保存
                //刷新所有CheckoutByMe的节点项
                hub.DesignTree.CheckinAllNodes();
                //清除所有签出缓存
                await StagedService.DeleteStagedAsync(txn);

                //先尝试递交第三方数据库的DDL事务
                foreach (var sqlTxn in otherStoreTxns.Values)
                {
                    var conn = sqlTxn.Connection;
                    sqlTxn.Commit();
                    conn.Dispose();
                }
                //再递交系统数据库事务
                await txn.CommitAsync();
            }
            catch (Exception) { throw; }
            finally
            {
                txn.Dispose();
                foreach (var sqlTxn in otherStoreTxns.Values)
                {
                    var conn = sqlTxn.Connection;
                    sqlTxn.Dispose();
                    if (conn != null)
                        conn.Dispose();
                }
            }

            //最后通知各节点更新模型缓存
            InvalidModelsCache(hub, package);
        }

        private static async ValueTask<DbTransaction> MakeOtherStoreTxn(ulong storeId, Dictionary<ulong, DbTransaction> txns)
        {
            DbTransaction txn;
            if (!txns.TryGetValue(storeId, out txn))
            {
                var sqlStore = SqlStore.Get(storeId);
                var conn = sqlStore.MakeConnection();
                await conn.OpenAsync();
                txn = conn.BeginTransaction();
                txns[storeId] = txn;
            }
            return txn;
        }

        private static async Task SaveModelsAsync(DesignHub hub, PublishPackage package, Transaction txn,
            Dictionary<ulong, DbTransaction> otherStoreTxns)
        {
            DbTransaction sqlTxn = null;

            //保存文件夹
            foreach (var folder in package.Folders)
            {
                await ModelStore.UpsertFolderAsync(folder, txn);
            }

            //保存模型，注意映射至系统存储的实体模型的变更与删除暂由ModelStore处理，映射至SqlStore的DDL暂在这里处理
            foreach (var model in package.Models)
            {
                switch (model.PersistentState)
                {
                    case PersistentState.Detached:
                        {
                            await ModelStore.InsertModelAsync(model, txn);
                            if (model.ModelType == ModelType.Entity)
                            {
                                var em = (EntityModel)model;
                                if (em.SqlStoreOptions != null) //映射至第三方数据库的需要创建相应的表
                                {
                                    var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                                    sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId, otherStoreTxns);
                                    await sqlStore.CreateTableAsync(em, sqlTxn, hub);
                                }
                            }
                            else if (model.ModelType == ModelType.View) //TODO:暂在这里保存视图模型的路由
                            {
                                var viewModel = (ViewModel)model;
                                if ((viewModel.Flag & ViewModelFlag.ListInRouter) == ViewModelFlag.ListInRouter)
                                {
                                    var app = hub.DesignTree.FindApplicationNode(model.AppId);
                                    var viewName = $"{app.Model.Name}.{viewModel.Name}";
                                    await ModelStore.UpsertViewRoute(viewName, viewModel.RoutePath, txn);
                                }
                            }
                            break;
                        }
                    case PersistentState.Unchanged: //TODO:临时
                    case PersistentState.Modified:
                        {
                            //TODO:判断服务及视图模型是否改名，是则将旧名称加入package内在下面删除掉旧的
                            await ModelStore.UpdateModelAsync(model, txn, aid => hub.DesignTree.FindApplicationNode(aid).Model);
                            if (model.ModelType == ModelType.Entity)
                            {
                                var em = (EntityModel)model;
                                if (em.SqlStoreOptions != null) //映射至第三方数据库的需要变更表
                                {
                                    var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                                    sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId, otherStoreTxns);
                                    await sqlStore.AlterTableAsync(em, sqlTxn, hub);
                                }
                            }
                            else if (model.ModelType == ModelType.View)
                            {
                                var viewModel = (ViewModel)model;
                                var app = hub.DesignTree.FindApplicationNode(model.AppId);
                                if ((viewModel.Flag & ViewModelFlag.ListInRouter) == ViewModelFlag.ListInRouter)
                                {
                                    var viewName = $"{app.Model.Name}.{viewModel.Name}";
                                    //TODO:判断重命名删除旧的
                                    await ModelStore.UpsertViewRoute(viewName, viewModel.RoutePath, txn);
                                }
                                else
                                {
                                    var oldViewName = $"{app.Model.Name}.{viewModel.OriginalName}";
                                    await ModelStore.DeleteViewRoute(oldViewName, txn);
                                }
                            }
                            break;
                        }
                    case PersistentState.Deleted:
                        {
                            await ModelStore.DeleteModelAsync(model, txn, aid => hub.DesignTree.FindApplicationNode(aid).Model);
                            
                            if (model.ModelType == ModelType.Entity)
                            {
                                var em = (EntityModel)model;
                                if (em.SqlStoreOptions != null) //映射至第三方数据库的需要删除相应的表
                                {
                                    var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                                    sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId, otherStoreTxns);
                                    await sqlStore.DropTableAsync(em, sqlTxn);
                                }
                            }
                            //判断模型类型删除相关代码及编译好的组件
                            else if (model.ModelType == ModelType.Service)
                            {
                                var app = hub.DesignTree.FindApplicationNode(model.AppId);
                                await ModelStore.DeleteModelCodeAsync(model.Id, txn);
                                await ModelStore.DeleteAssemblyAsync(true, $"{app.Model.Name}.{model.OriginalName}", txn);
                            }
                            else if (model.ModelType == ModelType.View)
                            {
                                var app = hub.DesignTree.FindApplicationNode(model.AppId);
                                var oldViewName = $"{app.Model.Name}.{model.OriginalName}";
                                await ModelStore.DeleteModelCodeAsync(model.Id, txn);
                                await ModelStore.DeleteAssemblyAsync(false, oldViewName, txn);
                                await ModelStore.DeleteViewRoute(oldViewName, txn);
                            }
                        }
                        break;
                }
            }

            //保存模型相关的代码
            foreach (var modelId in package.SourceCodes.Keys)
            {
                var codeData = package.SourceCodes[modelId];
                await ModelStore.UpsertModelCodeAsync(modelId, codeData, txn);
            }

            //保存服务模型编译好的组件
            foreach (var serviceName in package.ServiceAssemblies.Keys)
            {
                //TODO:Value=null的删除
                var asmData = package.ServiceAssemblies[serviceName];
                await ModelStore.UpsertAssemblyAsync(true, serviceName, asmData, txn);
            }

            //保存视图模型编译好的运行时代码
            foreach (var viewName in package.ViewAssemblies.Keys)
            {
                //TODO:Value=null的删除
                var asmData = package.ViewAssemblies[viewName];
                await ModelStore.UpsertAssemblyAsync(false, viewName, asmData, txn);
            }
        }

        /// <summary>
        /// 通知各节点模型缓存失效
        /// </summary>
        static void InvalidModelsCache(DesignHub hub, PublishPackage package)
        {
            if (package.Models.Count == 0)
                return;

            var others = package.Models.Where(t => t.ModelType != ModelType.Service).Select(t => t.Id).ToArray();
            var serviceModels = package.Models.Where(t => t.ModelType == ModelType.Service).Cast<ServiceModel>().ToArray();
            var services = new string[serviceModels.Length];
            for (int i = 0; i < serviceModels.Length; i++)
            {
                var sm = serviceModels[i];
                var app = hub.DesignTree.FindApplicationNode(sm.AppId).Model;
                services[i] = serviceModels[i].IsNameChanged ? $"{app.Name}.{sm.OriginalName}" : $"{app.Name}.{sm.Name}";
            }
            RuntimeContext.Current.InvalidModelsCache(services, others, true);
        }
    }
}
