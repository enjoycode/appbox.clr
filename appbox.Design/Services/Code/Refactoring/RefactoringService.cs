using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace appbox.Design
{
    /// <summary>
    /// 重构服务，主要用于查找引用及重命名
    /// </summary>
    static class RefactoringService
    {
        /// <summary>
        /// 查找模型的引用项
        /// </summary>
        internal static async Task<IList<Reference>> FindModelReferencesAsync(DesignHub ctx,
            ModelType modelType, string appName, string modelName)
        {
            switch (modelType)
            {
                case ModelType.Entity:
                    return await FindEntityReferences(ctx, appName, modelName);
                case ModelType.Service:
                    return await FindServiceReferences(ctx, appName, modelName);
                case ModelType.Report:
                case ModelType.View:
                    Log.Warn("查找引用尚未实现.");
                    return null;
                default:
                    throw ExceptionHelper.NotImplemented();
            }
        }

        internal static Task<List<Reference>> FindUsagesAsync(DesignHub hub,
                        ModelReferenceType referenceType, string appName, string modelName, string memberName)
        {
            return referenceType switch
            {
                ModelReferenceType.EntityMemberName => FindEntityMemberReferencesAsync(hub, appName, modelName, memberName),
                ModelReferenceType.EntityIndexName => FindEntityIndexReferencesAsync(hub, appName, modelName, memberName),
                ModelReferenceType.EnumModelItemName => FindEnumItemReferencesAsync(hub, appName, modelName, memberName),
                _ => throw new NotImplementedException(referenceType.ToString()),
            };
        }

        private static async Task<IList<Reference>> FindEntityReferences(DesignHub ctx, string appName, string modelName)
        {
            var ls = new List<Reference>();

            var modelClass = await ctx.TypeSystem.GetModelSymbolAsync(ModelType.Entity, appName, modelName);
            await AddCodeReferencesAsync(ctx, ls, modelClass, null);
            return ls;
        }

        /// <summary>
        /// 查找实体模型成员的引用项
        /// </summary>
        /// <returns>The none null entity member references.</returns>
        private static async Task<List<Reference>> FindEntityMemberReferencesAsync(DesignHub hub,
                                string appName, string modelName, string memberName)
        {
            if (string.IsNullOrEmpty(modelName))
                throw new ArgumentNullException(nameof(modelName), "实体模型标识为空");
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentNullException(nameof(memberName), "实体模型成员名称为空");

            var ls = new List<Reference>();

            //TODO:查找实体模型本身及所有其他实体模型的相关表达式（组织策略、编辑策略等）的引用
            //AddReferencesFromEntityModels(hub, ls, ModelReferenceType.EntityMemberName, modelID, memberName);

            //获取虚拟成员及相应的资源的虚拟成员
            var symbol = await hub.TypeSystem.GetEntityMemberSymbolAsync(appName, modelName, memberName);
            if (symbol != null)
                await AddCodeReferencesAsync(hub, ls, symbol.ContainingType, symbol);
            else
                Log.Warn($"Can't get EntityMember symbol: {appName}.{modelName}.{memberName}");

            return ls;
        }

        /// <summary>
        /// 查找实体模型的索引的引用项
        /// </summary>
        private static async Task<List<Reference>> FindEntityIndexReferencesAsync(DesignHub hub,
            string appName, string modelName, string indexName)
        {
            var ls = new List<Reference>();
            //获取索引虚拟成员
            var symbol = await hub.TypeSystem.GetEntityIndexSymbolAsync(appName, modelName, indexName);
            if (symbol != null)
                await AddCodeReferencesAsync(hub, ls, symbol, null);
            else
                Log.Warn($"Can't get EntityIndex symbol: {appName}.{modelName}.{indexName}");
            return ls;
        }


        private static async Task<IList<Reference>> FindServiceReferences(DesignHub ctx, string appName, string modelName)
        {
            var ls = new List<Reference>();

            //查找其他服务引用
            var modelClass = await ctx.TypeSystem.GetModelSymbolAsync(ModelType.Service, appName, modelName);
            await AddCodeReferencesAsync(ctx, ls, modelClass, null);
            //TODO:查找视图引用
            Log.Warn("查找视图等引用尚未实现.");
            return ls;
        }

        private static async Task<List<Reference>> FindEnumItemReferencesAsync(DesignHub hub,
                                string appName, string modelName, string memberName)
        {
            if (string.IsNullOrEmpty(modelName))
                throw new ArgumentNullException(nameof(modelName), "枚举模型标识为空");
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentNullException(nameof(memberName), "枚举模型成员名称为空");

            var ls = new List<Reference>();

            //TODO:待确认是否需要查找实体模型的引用
            //AddReferencesFromEntityModels(hub, ls, ModelReferenceType.EntityMemberName, modelID, memberName);

            //获取虚拟成员及相应的资源的虚拟成员
            var symbol = await hub.TypeSystem.GetEnumItemSymbolAsync(appName, modelName, memberName);
            //加入所有的代码引用
            if (symbol != null)
                await AddCodeReferencesAsync(hub, ls, symbol.ContainingType, symbol);
            else
                Log.Warn($"Can't get EnumItem symbol: {appName}.{modelName}.{memberName}");

            return ls;
        }

        /// <summary>
        /// 从所有实体模型中查找指定类型的引用项
        /// </summary>
        /// <param name="referenceType"></param>
        /// <param name="modelID"></param>
        /// <param name="memberName"></param>
        //private static void AddReferencesFromEntityModels(DesignHub hub, List<Reference> list,
        //    ModelReferenceType referenceType, string modelID, string memberName)
        //{
        //    var en = hub.DesignTree.FindNodesByType(ModelType.Entity);
        //    for (int i = 0; i < en.Length; i++)
        //    {
        //        ModelNode node = en[i];
        //        EntityModel model = (EntityModel)node.Model;
        //        List<ModelReferenceInfo> mrs = new List<ModelReferenceInfo>();
        //        model.AddModelReferences(mrs, referenceType, modelID, memberName);
        //        foreach (ModelReferenceInfo item in mrs)
        //        {
        //            list.Add(new ModelReference(model.ModelType, model.ID, item));
        //        }
        //    }
        //}

        /// <summary>
        /// 添加代码引用
        /// </summary>
        /// <param name="typeSymbol">目标类型</param>
        /// <param name="memberSymbol">目标成员类型，可为空.</param>
        private static async Task AddCodeReferencesAsync(DesignHub hub, List<Reference> list,
            INamedTypeSymbol typeSymbol, ISymbol memberSymbol)
        {
            var solution = hub.TypeSystem.Workspace.CurrentSolution;
            var targetSymbol = memberSymbol ?? typeSymbol;

            var mrefs = await SymbolFinder.FindReferencesAsync(targetSymbol, solution);
            foreach (var mref in mrefs)
            {
                foreach (var loc in mref.Locations)
                {
                    var docName = loc.Document.Name;
                    var sr = docName.Split('.');
                    var modelType = CodeHelper.GetModelTypeFromPluralString(sr[1]);
                    var appNode = hub.DesignTree.FindApplicationNodeByName(sr[0]);
                    var modelRootNode = appNode.FindModelRootNode(modelType);
                    var modelNode = modelRootNode.FindModelNodeByName(sr[2]);
                    var reference = new CodeReference(modelNode,
                        loc.Location.SourceSpan.Start, loc.Location.SourceSpan.Length);
                    list.Add(reference);
                }
            }
        }

        /// <summary>
        /// 开始执行重命名
        /// </summary>
        /// <param name="referenceType">Reference type.</param>
        /// <param name="modelID">Model identifier.</param>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        internal static async Task<string[]> RenameAsync(DesignHub hub, ModelReferenceType referenceType,
            ulong modelID, string oldName, string newName)
        {
            //注意：暂不用Roslyn的Renamer.RenameSymbolAsync，因为需要处理多个Symbol

            //Action<List<Reference>> addSpecRefsAction = null;
            ModelNode sourceNode;
            //1.先判断当前模型是否已签出
            switch (referenceType)
            {
                case ModelReferenceType.EntityMemberName:
                    sourceNode = hub.DesignTree.FindModelNode(ModelType.Entity, modelID);
                    break;
                default:
                    throw new NotImplementedException($"{referenceType}");
            }

            if (!sourceNode.IsCheckoutByMe)
                throw new Exception("当前模型尚未签出");

            //2.查找引用项并排序，同时判断有无签出
            var references = await FindUsagesAsync(hub, referenceType,
                sourceNode.AppNode.Model.Name, sourceNode.Model.Name, oldName);
            references.Sort();
            for (int i = 0; i < references.Count; i++)
            {
                if (!references[i].ModelNode.IsCheckoutByMe)
                    throw new Exception($"模型[{references[i].ModelNode.Model.Name}]尚未签出");
            }

            ////3.添加特殊引用项（如模型资源名称）
            //if (addSpecRefsAction != null)
            //    addSpecRefsAction(references);

            //4.开始重命名, TODO:考虑启用事务保存
            int diff = 0; //新旧成员名称间字符数之差的累积
            for (int i = 0; i < references.Count; i++)
            {
                Reference r = references[i];
                if (i > 0 && r.ModelNode.Model.Id == references[i - 1].ModelNode.Model.Id) //表示还在同一模型内
                {
                    diff += newName.Length - oldName.Length;
                }
                else //开始Rename新的模型
                {
                    //完结之前节点的重命名
                    if (i > 0)
                        await FinishRenamedNode(references[i - 1].ModelNode);
                    diff = 0;
                }

                //判断引用类型，分别处理
                switch (r)
                {
                    case CodeReference cr:
                        cr.Rename(hub, diff, newName);
                        break;
                    case ModelReference mr:
                        mr.TargetReference.Target.RenameReference(referenceType,
                            mr.TargetReference.TargetType, modelID, oldName, newName);
                        break;
                    default:
                        throw new Exception($"Unknown Reference Type: {r.GetType().Name}");
                }

                if (i == references.Count - 1)
                    await FinishRenamedNode(references[i].ModelNode);
            } //end for references

            //6.根据源类型进行相关的模型处理并保存，另根据源引用类型更新相应的RoslynDocument
            var needUpdateSourceRoslyn = false; //注意:如果改为RosylnRenamer实现则不再需要更新
            switch (referenceType)
            {
                case ModelReferenceType.EntityMemberName:
                    ((EntityModel)sourceNode.Model).RenameMember(oldName, newName);
                    needUpdateSourceRoslyn = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
            await sourceNode.SaveAsync(null);
            if (needUpdateSourceRoslyn)
                await hub.TypeSystem.UpdateModelDocumentAsync(sourceNode);

            //最后返回处理结果，暂简单返回受影响的节点，由前端刷新
            return references.Select(t => t.ModelNode.Model.Id.ToString()).Distinct().ToArray();
        }

        /// <summary>
        /// RenameReferences的辅助方法，用于完结模型结点的重命名，并保存模型
        /// </summary>
        private static async Task FinishRenamedNode(ModelNode node)
        {
            //保存节点
            await node.SaveAsync(null);
            //TODO: 是否需要更新引用者的RoslynDocument
        }

        #region ----尝试使用Roslyn.Renamer----
        // private static void RenameEntityMember(DesignHub hub, ModelReferenceType referenceType,
        //     string modelID, string oldName, string newName)
        // {
        //     //1.先判断名称合法性

        //     //2.先尝试签出当前节点
        //     var sourceNode = hub.DesignTree.FindModelNode(ModelType.Entity, modelID);
        //     if (!sourceNode.Checkout())
        //         throw new Exception("无法签出当前模型");
        //     //3.查找引用并尝试签出相关节点
        //     var references = FindUsages(hub, referenceType, modelID, oldName);
        //     for (int i = 0; i < references.Count; i++)
        //     {
        //         var designNode = hub.DesignTree.FindModelNode(references[i].ModelType, references[i].ModelID);
        //         if (!designNode.Checkout())
        //             throw new Exception($"无法签出模型[{references[i].ModelID}]");
        //     }
        //     //4.重命名虚拟代码引用
        //     var workspace = hub.TypeSystem.Workspace;
        //     var symbols = hub.TypeSystem.GetEntityMemberSymbols(modelID, oldName);
        //     for (int i = 0; i < symbols.Length; i++)
        //     {
        //         var update = Renamer.RenameSymbolAsync(workspace.CurrentSolution,
        //             symbols[i], newName, workspace.Options).Result;
        //         //重新验证受影响的节点是否签出
        //         //todo:暂无法处理两个symbol一个应用更新成功，另一个失败的问题
        //         if (!workspace.TryApplyChanges(update))
        //             throw new Exception("无法进行重命名操作");
        //     }
        //     //5.重命名模型引用，如表达式内的引用

        //     //6.启用事务保存所有受影响的节点
        //     using (var ts = SqlStore.Default.NewTransactionScope())
        //     {
        //         var model = (EntityModel)sourceNode.Model;
        //         model.RenameMember(oldName, newName);
        //         sourceNode.Save(null);

        //         ts.Complete();
        //     }
        // }
        #endregion
    }
}
