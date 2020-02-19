using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using appbox.Models;
using appbox.Caching;
using appbox.Design.ServiceInterceptors;

namespace appbox.Design
{
    /// <summary>
    /// 用于生成运行时的服务代码
    /// </summary>
    sealed class ServiceCodeGenerator : CSharpSyntaxRewriter
    {
        //TODO: 优化所有的SyntaxFactory.ParseXXX实现
        //TODO: 抛出转换异常包含虚拟代码位置信息

        #region ====Statics====
        private static readonly Dictionary<string, IInvocationInterceptor<SyntaxNode>> invocationInterceptors;
        private static readonly Dictionary<string, IMemberAccessInterceptor<SyntaxNode>> memberAccessInterceptors;

        static ServiceCodeGenerator()
        {
            invocationInterceptors = new Dictionary<string, IInvocationInterceptor<SyntaxNode>>
            {
                { CallServiceInterceptor.Name, new CallServiceInterceptor() },
                { PartitionPredicateInterceptor.Name, new PartitionPredicateInterceptor() },
                { IndexPredicateInterceptor.Name, new IndexPredicateInterceptor() },
                { ToTreeListInterceptor.Name, new ToTreeListInterceptor() },
                { IncludeInterceptor.Name, new IncludeInterceptor() },
                { LoadEntityInterceptor.Name, new LoadEntityInterceptor() },
                { LoadEntitySetInterceptor.Name, new LoadEntitySetInterceptor() },
                { DeleteEntityInterceptor.Name, new DeleteEntityInterceptor() },
                { CqlWhereInterceptor.Name, new CqlWhereInterceptor() },
                { CqlToEntityListInterceptor.Name, new CqlToEntityListInterceptor() },
                { CqlToListInterceptor.Name, new CqlToListInterceptor() }
            };

            memberAccessInterceptors = new Dictionary<string, IMemberAccessInterceptor<SyntaxNode>>
            {
                { PermissionAccessInterceptor.Name, new PermissionAccessInterceptor() }
            };
        }

        private static IInvocationInterceptor<SyntaxNode> GetInvocationInterceptor(ISymbol symbol)
        {
            if (symbol != null)
            {
                var attributes = symbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == TypeHelper.InvocationInterceptorAttribute)
                    {
                        var key = item.ConstructorArguments[0].Value.ToString();
                        if (!invocationInterceptors.TryGetValue(key, out IInvocationInterceptor<SyntaxNode> interceptor))
                            Log.Debug($"未能找到InvocationInterceptor: {key}");
                        return interceptor;
                    }
                }
            }
            return null;
        }

        private static IMemberAccessInterceptor<SyntaxNode> GetMemberAccessInterceptor(ISymbol symbol)
        {
            if (symbol != null)
            {
                var attributes = symbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == TypeHelper.MemberAccessInterceptorAttribute)
                    {
                        var key = item.ConstructorArguments[0].Value.ToString();
                        if (!memberAccessInterceptors.TryGetValue(key, out IMemberAccessInterceptor<SyntaxNode> interceptor))
                            Log.Debug($"未能找到MemberAccessInterceptro: {key}");
                        return interceptor;
                    }
                }
            }
            return null;
        }
        #endregion

        #region ====Fields & Properties====
        private QueryMethodContext queryMethodCtx; //用于SqlQuery处理
        internal string cqlFilterLambdaParameter = null; //用于VisitMemberAccess判断是否CqlQuery的Filter Lambda

        /// <summary>
        /// 公开的服务方法集合
        /// </summary>
        private readonly List<MethodDeclarationSyntax> publicMethods = new List<MethodDeclarationSyntax>();
        /// <summary>
        /// 公开的服务方法的调用权限，key=方法名称，value=已经生成的验证代码
        /// </summary>
        private readonly Dictionary<string, string> publicMethodsInvokePermissions = new Dictionary<string, string>();

        public SemanticModel SemanticModel { get; private set; }
        /// <summary>
        /// 需要编译的服务模型
        /// </summary>
        public ServiceModel ServiceModel { get; private set; }

        public string AppName { get; private set; }

        internal readonly DesignHub hub;
        #endregion

        #region ====Ctor====
        public ServiceCodeGenerator(DesignHub hub, string appName, SemanticModel semanticModel, ServiceModel serviceModel)
        {
            this.hub = hub;
            AppName = appName;
            SemanticModel = semanticModel;
            ServiceModel = serviceModel;
            queryMethodCtx = new QueryMethodContext();
        }
        #endregion

        #region ====ObjectCreation====
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            //TODO:重构改用拦截器处理
            //TODO:***暂不支持实体类的初始化，待解决 new Entities.Customer() { Name = "aaa" }
            var typeSymbol = SemanticModel.GetSymbolInfo(node.Type).Symbol as INamedTypeSymbol;
            AttributeData realType = null;
            if (TypeHelper.IsEntityClass(typeSymbol))
            {
                string[] names = typeSymbol.ToString().Split('.');
                var appNode = hub.DesignTree.FindApplicationNodeByName(names[0]);
                var entityModelNode = hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
                var entityModelId = entityModelNode.Model.Id;
                var res = (ObjectCreationExpressionSyntax)SyntaxFactory.ParseExpression($"new {TypeHelper.RuntimeType_Entity}({entityModelId})");
                if (node.ArgumentList != null && node.ArgumentList.Arguments.Count > 0)
                {
                    for (int i = 0; i < node.ArgumentList.Arguments.Count; i++)
                    {
                        var arg = (ArgumentSyntax)node.ArgumentList.Arguments[i].Accept(this);
                        var argList = res.ArgumentList.AddArguments(arg);
                        res = res.WithArgumentList(argList);
                    }
                }
                return res;
            }
            if (TypeHelper.IsGenericCreateClass(typeSymbol, ref realType))
            {
                var typeArgs = ((ITypeSymbol)typeSymbol).GetTypeArguments();
                var modelType = typeArgs[0];
                var realTypeName = (string)realType.ConstructorArguments[0].Value;
                string[] names = modelType.ToString().Split('.');
                var appNode = hub.DesignTree.FindApplicationNodeByName(names[0]);
                var entityModelNode = hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
                var model = (EntityModel)entityModelNode.Model;
                //TODO:暂丑陋的判断IndexScan，待重构
                if (typeArgs.Count() == 1)
                    return SyntaxFactory.ParseExpression($"new {realTypeName}({model.Id})").WithTriviaFrom(node);

                var indexType = typeArgs[1];
                var indexId = model.SysStoreOptions.Indexes.Single(t => t.Name == indexType.Name).IndexId;
                return SyntaxFactory.ParseExpression($"new {realTypeName}({model.Id}, {indexId})").WithTriviaFrom(node);
            }
            if (TypeHelper.IsWorkflowClass(typeSymbol))
            {
                string[] names = typeSymbol.ToString().Split('.');
                return SyntaxFactory.ParseExpression($"new AppBox.Core.WorkflowInstanceInfo(\"{names[0]}.{names[2]}\")");
            }
            else
            {
                var realTypeName = TypeHelper.GetRealTypeName(typeSymbol);
                if (realTypeName != null)
                {
                    if (typeSymbol.IsGenericType) //范型类单独处理
                    {
                        return node.WithType(TypeHelper.ConvertToRuntimeType(typeSymbol));
                    }
                    var update = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node);
                    return update.WithType(TypeHelper.GetRealType(realTypeName));
                }
                return base.VisitObjectCreationExpression(node);
            }
        }
        #endregion

        #region ====MemberAccessExpression====
        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            //1. 先处理查询类方法的lambda表达式内的实体成员访问
            if (queryMethodCtx.HasAny && queryMethodCtx.Current.InLambdaExpression) //t.Customer.Name
            {
                var identifier = FindIndentifierForMemberAccessExpression(node);
                if (identifier != null)
                {
                    var replacedIdentifier = queryMethodCtx.Current.ReplaceLambdaParameter(identifier);
                    if (replacedIdentifier != null)
                    {
                        var sb = StringBuilderCache.Acquire();
                        BuildQueryMethodMemberAccess(node, replacedIdentifier, sb);
                        //TODO:判断是否由上级处理换行
                        //return SyntaxFactory.ParseExpression(sb.ToString()).WithTrailingTrivia(GetEndOfLineTrivia(node, false));
                        return SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb)).WithTriviaFrom(node);
                    }
                }
            }
            else if (cqlFilterLambdaParameter != null)
            {
                if (node.Expression is IdentifierNameSyntax identifier
                    && identifier.Identifier.ValueText == cqlFilterLambdaParameter)
                {
                    var sb = StringBuilderCache.Acquire();
                    CqlLambdaHelper.BuildCqlLambdaGetValue(sb, cqlFilterLambdaParameter,
                        -1, node.Name.Identifier.ValueText, node, SemanticModel);
                    return SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb));
                }
            }

            var expSymbol = SemanticModel.GetSymbolInfo(node).Symbol;
            //2. 判断有无拦截器
            var interceptor = GetMemberAccessInterceptor(expSymbol);
            if (interceptor != null)
                return interceptor.VisitMemberAccess(node, expSymbol, this);

            //3. 正常处理成员访问
            if (expSymbol != null)
            {
                //先处理存储类的属性或方法
                var storeClass = TypeHelper.IsDataStoreClass(expSymbol.ContainingType);
                if (storeClass != null)
                {
                    //根据DataStore名称找到相应的节点
                    var storeName = ((MemberAccessExpressionSyntax)node.Expression).Name;
                    var storeNode = hub.DesignTree.FindDataStoreNodeByName(storeName.ToString());
                    var updateNode = SyntaxFactory.ParseExpression(string.Format("appbox.Store.{0}.Get({1}ul).{2}",
                        storeClass, storeNode.Model.Id, node.Name));
                    return updateNode.WithTriviaFrom(node);
                }

                if (expSymbol.IsStatic && expSymbol is IMethodSymbol) //方法名称处理
                {
                    //处理需要转换类型的静态方法访问
                    var realTypeName = TypeHelper.GetRealTypeName(expSymbol.ContainingType);
                    if (!string.IsNullOrEmpty(realTypeName))
                        return SyntaxFactory.ParseExpression($"{realTypeName}.{node.Name.Identifier.ValueText}");
                }
                else //非方法名称处理
                {
                    if (TypeHelper.IsEntityClass(expSymbol.ContainingType)) //处理实体成员访问
                    {
                        //先处理TypeId静态成员(TODO:改用拦截器处理)
                        if (node.Name.Identifier.ValueText == "TypeId")
                        {
                            var names = expSymbol.ContainingType.ToString().Split('.');
                            var appNode = hub.DesignTree.FindApplicationNodeByName(names[0]);
                            var entityModelNode = hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
                            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                                   SyntaxFactory.Literal(entityModelNode.Model.Id));
                        }

                        //判断成员是否属于实体成员
                        if (!expSymbol.ContainingType.ToString().In(
                            new string[] { TypeHelper.Type_EntityBase, TypeHelper.Type_SysEntityBase,
                                TypeHelper.Type_SqlEntityBase, TypeHelper.Type_CqlEntityBase }))
                        {
                            //TODO:判断是否AggregationRefField，返回的object类型，应使用BoxedValue处理
                            ITypeSymbol valueTypeSymbol = TypeHelper.GetSymbolType(expSymbol);
                            var memberId = GetEntityMemberId(expSymbol);

                            var oldTarget = (ExpressionSyntax)Visit(node.Expression);
                            //TODO: cache methodName
                            var methodName = (SimpleNameSyntax)SyntaxFactory.ParseName(TypeHelper.GenEntityMemberGetterOrSetter(valueTypeSymbol, true));
                            var getValueMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, oldTarget, methodName);
                            var arg1 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                                                              SyntaxFactory.Literal(memberId)));
                            var argList = SyntaxFactory.ArgumentList().AddArguments(arg1);
                            return SyntaxFactory.InvocationExpression(getValueMethod, argList);
                        }
                    }
                    else if (TypeHelper.IsEnumModel(expSymbol.ContainingType)) //处理枚举成员访问
                    {
                        throw ExceptionHelper.NotImplemented();
                        //var names = expSymbol.ContainingType.ToString().Split('.');
                        //var enumModel = DesignHelper.DesignTimeModelContainer.GetEnumModel(names[0] + "." + names[2]);
                        //int enumValue = 0;
                        //for (int i = 0; i < enumModel.Items.Count; i++)
                        //{
                        //    if (enumModel.Items[i].Name == node.Name.Identifier.ValueText)
                        //    {
                        //        enumValue = enumModel.Items[i].Value;
                        //    }
                        //}
                        //return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        //SyntaxFactory.Literal(enumValue));
                    }
                    else if (expSymbol.IsStatic && (expSymbol is IPropertySymbol || expSymbol is IFieldSymbol))
                    {
                        //处理需要转换为运行时类型的静态成员访问, eg: PersistentState.Detached
                        var realTypeName = TypeHelper.GetRealTypeName(expSymbol.ContainingType);
                        if (!string.IsNullOrEmpty(realTypeName))
                            return SyntaxFactory.ParseExpression($"{realTypeName}.{node.Name.Identifier.ValueText}");
                    }
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        private IdentifierNameSyntax FindIndentifierForMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax)
                return (IdentifierNameSyntax)node.Expression;

            if (node.Expression is MemberAccessExpressionSyntax)
                return FindIndentifierForMemberAccessExpression((MemberAccessExpressionSyntax)node.Expression);

            return null;
        }

        private void BuildQueryMethodMemberAccess(MemberAccessExpressionSyntax node, IdentifierNameSyntax targetIdentifier, StringBuilder sb)
        {
            //根据是否在Sql查询内使用不同的处理方式
            if (queryMethodCtx.Current.IsSystemQuery)
            {
                if (node.Expression is IdentifierNameSyntax)
                {
                    var expSymbol = SemanticModel.GetSymbolInfo(node).Symbol;
                    var memberId = GetEntityMemberId(expSymbol);
                    var valueType = ((IPropertySymbol)expSymbol).Type;
                    var valueTypeName = TypeHelper.GetEntityMemberTypeString(valueType, out _);

                    sb.Insert(0, $"{targetIdentifier.Identifier.ValueText}.Get{valueTypeName}({memberId})");
                }
                else if (node.Expression is MemberAccessExpressionSyntax)
                {
                    BuildQueryMethodMemberAccess((MemberAccessExpressionSyntax)node.Expression, targetIdentifier, sb);
                    sb.AppendFormat(".{0}", node.Name.Identifier.ValueText);
                }
            }
            else
            {
                string sep = queryMethodCtx.Current.IsIncludeMethod ? "" : ".T"; //Include类方法不需要.T
                if (node.Expression is IdentifierNameSyntax)
                {
                    sb.Insert(0, $"{targetIdentifier.Identifier.ValueText}{sep}[\"{node.Name.Identifier.ValueText}\"]");
                }
                else if (node.Expression is MemberAccessExpressionSyntax)
                {
                    BuildQueryMethodMemberAccess((MemberAccessExpressionSyntax)node.Expression, targetIdentifier, sb);

                    var symbol = SemanticModel.GetSymbolInfo(node).Symbol;
                    if (TypeHelper.IsEntityClass(symbol.ContainingSymbol as INamedTypeSymbol)) //TODO: 暂用该方式判断是否Entity的成员
                        sb.AppendFormat("[\"{0}\"]", node.Name.Identifier.ValueText);
                    else
                        sb.AppendFormat(".{0}", node.Name.Identifier.ValueText);
                }
            }
        }
        #endregion

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (queryMethodCtx.HasAny && queryMethodCtx.Current.InLambdaExpression)
            {
                //如果在条件表达式内将&&转为&, ||转为|
                if (node.OperatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken) //&& -> &
                {
                    var left = (ExpressionSyntax)node.Left.Accept(this).WithTriviaFrom(node.Left);
                    var right = (ExpressionSyntax)node.Right.Accept(this).WithTriviaFrom(node.Right);
                    return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseAndExpression, left, right).WithTriviaFrom(node);
                }
                if (node.OperatorToken.Kind() == SyntaxKind.BarBarToken) //|| -> |
                {
                    var left = (ExpressionSyntax)node.Left.Accept(this);
                    var right = (ExpressionSyntax)node.Right.Accept(this);
                    return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, left, right).WithTriviaFrom(node);
                }
                //TODO:转换其他Bitwise操作符
                if (node.OperatorToken.Kind() == SyntaxKind.AmpersandToken
                    || node.OperatorToken.Kind() == SyntaxKind.BarToken)
                {
                    throw new NotImplementedException("Binary & and | operator not implemented.");
                }
            }
            return base.VisitBinaryExpression(node);
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            //先处理查询类方法内的LambdaExpression，目前主要是UpdateCommand.Update
            if (queryMethodCtx.HasAny && queryMethodCtx.Current.InLambdaExpression)
            {
                //cmd.Update(t => t.Value = t.Value + 1) 转换为 cmd.Update(cmd.T["Value"].Assign(cmd.T["Value"] + 1))
                var left = (ExpressionSyntax)node.Left.Accept(this);
                var right = (ExpressionSyntax)node.Right.Accept(this);

                var methodName = (SimpleNameSyntax)SyntaxFactory.ParseName("Assign");
                var assignMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, methodName);
                var arg1 = SyntaxFactory.Argument(right);
                var argList = SyntaxFactory.ArgumentList().AddArguments(arg1);
                return SyntaxFactory.InvocationExpression(assignMethod, argList);
            }

            //判断左侧是否是EntityMember
            var expSymbol = SemanticModel.GetSymbolInfo(node.Left).Symbol;
            if (expSymbol != null)
            {
                if (TypeHelper.IsEntityClass(expSymbol.ContainingType))
                {
                    ITypeSymbol valueTypeSymbol = TypeHelper.GetSymbolType(expSymbol);

                    var memberAccess = (MemberAccessExpressionSyntax)node.Left;
                    var oldTarget = (ExpressionSyntax)Visit(memberAccess.Expression);

                    //注意处理 = null的问题
                    var memberId = GetEntityMemberId(expSymbol);
                    var arg1 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                                                      SyntaxFactory.Literal(memberId)));
                    var arg2 = SyntaxFactory.Argument((ExpressionSyntax)Visit(node.Right));
                    SimpleNameSyntax methodName = (SimpleNameSyntax)SyntaxFactory.ParseName(TypeHelper.GenEntityMemberGetterOrSetter(valueTypeSymbol, false));
                    ArgumentListSyntax argList = SyntaxFactory.ArgumentList().AddArguments(arg1, arg2);

                    var setValueMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, oldTarget, methodName);
                    return SyntaxFactory.InvocationExpression(setValueMethod, argList);
                }
                else if (TypeHelper.IsWorkflowClass(expSymbol.ContainingType))
                {
                    var memberAccess = (MemberAccessExpressionSyntax)node.Left;
                    var oldTarget = (ExpressionSyntax)Visit(memberAccess.Expression);
                    var propertyName = (SimpleNameSyntax)SyntaxFactory.ParseName("SetBoxedPropertyValue");
                    var setValueMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, oldTarget, propertyName);
                    var arg1 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                                                                      SyntaxFactory.Literal(memberAccess.Name.Identifier.ValueText)));
                    var arg2 = SyntaxFactory.Argument((ExpressionSyntax)Visit(node.Right));
                    var argList = SyntaxFactory.ArgumentList().AddArguments(arg1, arg2);

                    return SyntaxFactory.InvocationExpression(setValueMethod, argList);
                }
            }

            return base.VisitAssignmentExpression(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var methodSymbol = SemanticModel.GetSymbolInfo(node.Expression).Symbol as IMethodSymbol;
            //先判断有无拦截器
            var interceptor = GetInvocationInterceptor(methodSymbol);
            if (interceptor != null)
                return interceptor.VisitInvocation(node, methodSymbol, this);

            //再判断是否在QueryMethod的Lambda内
            //if (queryMethodCtx.HasAny && queryMethodCtx.Current.InLambdaExpression)
            //    return base.VisitInvocationExpression(node);

            bool needPopQueryMethod = false;
            if (methodSymbol != null)
            {
                var ownerType = methodSymbol.ContainingType;
                if (TypeHelper.IsQuerialbeClass(ownerType, out bool isSystemQuery)
                    && TypeHelper.IsQueryMethod(methodSymbol)) //注意:只处理相关QueryMethods
                {
                    //设置当前查询方法上下文
                    var queryMethod = new QueryMethod()
                    {
                        IsSystemQuery = isSystemQuery, //是否系统存储的查询，否则是Sql查询
                        MethodName = methodSymbol.Name,
                        ArgsCount = node.ArgumentList.Arguments.Count,
                        Identifiers = null,
                        LambdaParameters = null
                    };
                    queryMethodCtx.Push(queryMethod); //支持嵌套
                    needPopQueryMethod = true;

                    if (!queryMethod.IsIncludeMethod) //排除Include类方法
                    {
                        //注意：目前只支持所有的非Lambda参数为IdentifierNameSyntax
                        queryMethod.Identifiers = new IdentifierNameSyntax[queryMethod.ArgsCount];
                        queryMethod.LambdaParameters = new string[queryMethod.ArgsCount];
                        queryMethod.Identifiers[0] = GetIdentifier(node.Expression); //指向自己
                        if (queryMethod.ArgsCount > 1)
                        {
                            //注意：这里不移除无效的参数节点，由VisitArgumentList()处理
                            for (int i = 0; i < queryMethod.ArgsCount - 1; i++)
                            {
                                queryMethod.Identifiers[i + 1] = (IdentifierNameSyntax)node.ArgumentList.Arguments[i].Expression;
                            }
                        }
                    }
                }
                else if (methodSymbol.Name == "ToString" && ownerType.ToString() == "System.Enum") //处理虚拟枚举值的ToString
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var ownerSymbol = SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                        if (ownerSymbol is IFieldSymbol) //eg: sys.Enums.Gender.Male.ToString() => "Male"
                        {
                            if (TypeHelper.IsEnumModel(ownerSymbol.ContainingType))
                                return SyntaxFactory.ParseExpression($"\"{ownerSymbol.Name}\"").WithTriviaFrom(node);
                        }
                        else
                        {
                            throw new NotImplementedException();
                            //var enumType = TypeHelper.GetSymbolType(ownerSymbol);
                            //if (TypeHelper.IsEnumModel(enumType as INamedTypeSymbol))
                            //{
                            //    var updateExp = memberAccess.Expression.Accept(this);
                            //    var sr = enumType.ToString().Split('.');
                            //    var newexp = $"AppBox.Core.EnumModel.ToString(AppBox.Core.RuntimeContext.Default,\"{sr[0]}.{sr[2]}\",{updateExp.ToString()})";
                            //    return SyntaxFactory.ParseExpression(newexp).WithTriviaFrom(node);
                            //}
                        }
                    }
                }
                //TODO:处理Enum.Parse等方法
            }

            var res = (InvocationExpressionSyntax)base.VisitInvocationExpression(node);
            if (needPopQueryMethod)
            {
                //注意：将ToScalar转换为ToScalar<T>
                if (queryMethodCtx.Current.MethodName == TypeHelper.SqlQueryToScalarMethod)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                    var newGenericName = (SimpleNameSyntax)SyntaxFactory.ParseName($"ToScalar<{methodSymbol.TypeArguments[0]}>");
                    memberAccess = memberAccess.WithName(newGenericName);
                    res = res.WithExpression(memberAccess);
                }
                queryMethodCtx.Pop();
            }

            return res;
        }

        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        {
            //注意：相关Query.XXXJoin(join, (u, j1) => u.ID == j1.OtherID)不处理，因本身需要之前的参数
            if (queryMethodCtx.HasAny && !queryMethodCtx.Current.HoldLambdaArgs && !queryMethodCtx.Current.InLambdaExpression)
            {
                var args = new SeparatedSyntaxList<ArgumentSyntax>();
                //eg: q.Where(join1, (u, j1) => u.ID == j1.OtherID)
                //注意：只处理最后一个参数，即移除之前的参数，如上示例中的join1参数，最后的Lambda由VisitQueryMethodLambdaExpresssion处理
                var newArgNode = node.Arguments[node.Arguments.Count - 1].Expression.Accept(this);

                //eg: q.ToList(join1, (t, j1) => new {t.ID, t.Name, j1.Address})
                //需要处理 new {XX,XX,XX}为参数列表
                if ((queryMethodCtx.Current.MethodName == TypeHelper.SqlQueryToScalarMethod
                    || queryMethodCtx.Current.MethodName == TypeHelper.SqlQueryToListMethod
                    || queryMethodCtx.Current.MethodName == TypeHelper.SqlUpdateOutputMethod)
                    && newArgNode is ArgumentListSyntax argList)
                {
                    //已被VisitQueryMethodLambdaExpresssion转换为ArgumentListSyntax
                    args = args.AddRange(argList.Arguments);
                }
                else
                {
                    args = args.Add(SyntaxFactory.Argument((ExpressionSyntax)newArgNode));
                }

                return SyntaxFactory.ArgumentList(args);
            }

            return base.VisitArgumentList(node);
        }

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            var typeSymbol = SemanticModel.GetSymbolInfo(node.Type).Symbol;
            if (TypeHelper.IsEntityClass(typeSymbol as INamedTypeSymbol))
            {
                var updateNode = (CastExpressionSyntax)base.VisitCastExpression(node);
                return updateNode.WithType(TypeHelper.EntityTypeSyntax);
            }
            if (TypeHelper.IsEnumModel(typeSymbol as INamedTypeSymbol))
            {
                //todo:暂转换为int类型
                return node.WithType(SyntaxFactory.ParseTypeName("int"));
            }

            return base.VisitCastExpression(node);
        }

        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            //TODO:暂全部移除Attribute
            base.VisitAttributeList(node);
            return node.RemoveNodes(node.ChildNodes(), SyntaxRemoveOptions.KeepEndOfLine);
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            var methodDeclaration = node.Parent.Parent as MethodDeclarationSyntax;
            if (TypeHelper.IsServiceMethod(methodDeclaration)) //服务方法特殊Attribute处理
            {
                var symbol = SemanticModel.GetSymbolInfo(node.Name).Symbol;
                if (symbol != null && symbol.ContainingType != null &&
                    symbol.ContainingType.ToString() == TypeHelper.InvokePermissionAttribute)
                {
                    //TODO:***处理系统特殊权限,如流程引擎sys.Permissions.WorkflowEngine
                    var arg = node.ArgumentList.ChildNodes().First() as AttributeArgumentSyntax;
                    var source = Visit(arg.Expression);
                    publicMethodsInvokePermissions.Add(methodDeclaration.Identifier.ValueText, source.ToString());
                    return null; //TODO:暂直接返回null
                }
            }

            return base.VisitAttribute(node);
        }

        // 用于转换范型<XX.Entities.XXX>
        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            var newargs = SyntaxFactory.SeparatedList<TypeSyntax>();
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                var typeSymbol = SemanticModel.GetSymbolInfo(node.Arguments[i]).Symbol;
                //todo:考虑使用ConvertToRuntimeType()
                if (TypeHelper.IsEntityClass(typeSymbol as INamedTypeSymbol))
                    newargs = newargs.Add(TypeHelper.EntityTypeSyntax);
                else
                    newargs = newargs.Add(node.Arguments[i]);
            }

            return SyntaxFactory.TypeArgumentList(newargs);
        }

        #region ====LambdaExpression====
        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (queryMethodCtx.HasAny)
            {
                if (queryMethodCtx.Current.IsIncludeMethod) //暂Include类特殊处理
                {
                    queryMethodCtx.Current.InLambdaExpression = true;
                    var res = base.VisitSimpleLambdaExpression(node);
                    queryMethodCtx.Current.InLambdaExpression = false;
                    return res;
                }
                else
                {
                    queryMethodCtx.Current.LambdaParameters[0] = node.Parameter.Identifier.ValueText;
                    //TODO:考虑预先处理 t=> 行差
                    return VisitQueryMethodLambdaExpresssion(node);
                }
            }

            return base.VisitSimpleLambdaExpression(node);
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (queryMethodCtx.HasAny)
            {
                if (queryMethodCtx.Current.IsIncludeMethod) //暂Include类特殊处理
                {
                    queryMethodCtx.Current.InLambdaExpression = true;
                    var res = base.VisitParenthesizedLambdaExpression(node);
                    queryMethodCtx.Current.InLambdaExpression = false;
                    return res;
                }
                else
                {
                    for (int i = 0; i < node.ParameterList.Parameters.Count; i++)
                    {
                        queryMethodCtx.Current.LambdaParameters[i] = node.ParameterList.Parameters[i].Identifier.ValueText;
                    }
                    //TODO:考虑预先处理 (t)=> 行差
                    return VisitQueryMethodLambdaExpresssion(node);
                }
            }

            return base.VisitParenthesizedLambdaExpression(node);
        }

        private SyntaxNode VisitQueryMethodLambdaExpresssion(LambdaExpressionSyntax lambda)
        {
            queryMethodCtx.Current.InLambdaExpression = true;
            SyntaxNode res;
            if (queryMethodCtx.Current.IsDynamicMethod)
            {
                //注意处理行差
                var args = new SeparatedSyntaxList<ArgumentSyntax>();
                if (lambda.Body is AnonymousObjectCreationExpressionSyntax aoc)
                {
                    //转换Lambda表达式为运行时Lambda表达式
                    //eg: t=>new {t.Id, t.Name} 转换为 r=> new {Id=r.GetInt(0), Name=r.GetString(1)}
                    var sb = StringBuilderCache.Acquire();
                    sb.Append("r => new {");
                    for (int i = 0; i < aoc.Initializers.Count; i++)
                    {
                        if (i != 0) sb.Append(',');
                        var initializer = aoc.Initializers[i];
                        if (initializer.NameEquals != null)
                            sb.Append(initializer.NameEquals.Name.Identifier.ValueText);
                        else
                            sb.Append(((MemberAccessExpressionSyntax)initializer.Expression).Name.Identifier.ValueText);
                        sb.Append("=r.Get");
                        var expSymbol = SemanticModel.GetSymbolInfo(initializer.Expression).Symbol;
                        var expType = TypeHelper.GetSymbolType(expSymbol);
                        var typeString = TypeHelper.GetEntityMemberTypeString(expType, out bool isNullable);
                        if (isNullable) sb.Append("Nullable");
                        sb.Append(typeString);
                        sb.Append('(');
                        sb.Append(i);
                        sb.Append(')');
                    }
                    sb.Append('}');
                    //转换为参数并加入参数列表
                    args = args.Add(SyntaxFactory.Argument(
                            SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb))
                        ));

                    //处理selectItems参数
                    for (int i = 0; i < aoc.Initializers.Count; i++)
                    {
                        var initializer = aoc.Initializers[i];
                        var argExpression = (ExpressionSyntax)initializer.Expression.Accept(this);
                        if (initializer.NameEquals != null) //TODO:***检查是否还需要转换为SelectAs("XXX")，因前面已按序号获取
                        {
                            var selectAsMethodName = (SimpleNameSyntax)SyntaxFactory.ParseName("SelectAs");
                            var selectAsMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argExpression, selectAsMethodName);
                            var selectAsArgs = SyntaxFactory.ParseArgumentList(string.Format("(\"{0}\")", initializer.NameEquals.Name.Identifier.ValueText));
                            argExpression = SyntaxFactory.InvocationExpression(selectAsMethod, selectAsArgs);
                        }
                        var arg = SyntaxFactory.Argument(argExpression);
                        //最后一个参数补body所有行差
                        if (i == aoc.Initializers.Count - 1)
                        {
                            var lineSpan = lambda.Body.GetLocation().GetLineSpan();
                            var lineDiff = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;
                            if (lineDiff > 0)
                                arg = arg.WithTrailingTrivia(SyntaxFactory.Whitespace(new string('\n', lineDiff)));
                        }
                        args = args.Add(arg);
                    }
                }
                else if (lambda.Body is MemberAccessExpressionSyntax ma)
                {
                    //转换Lambda表达式为运行时Lambda表达式
                    //eg: t=> t.Name 转换为 r=> r.GetString(0)
                    var sb = StringBuilderCache.Acquire();
                    sb.Append("r => r.Get");
                    var expSymbol = SemanticModel.GetSymbolInfo(ma).Symbol;
                    var expType = TypeHelper.GetSymbolType(expSymbol);
                    var typeString = TypeHelper.GetEntityMemberTypeString(expType, out bool isNullable);
                    if (isNullable) sb.Append("Nullable");
                    sb.Append(typeString);
                    sb.Append("(0)");
                    //转换为参数并加入参数列表
                    args = args.Add(SyntaxFactory.Argument(
                            SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb))
                        ));

                    //处理selectItems参数
                    var argExpression = (ExpressionSyntax) ma.Accept(this);
                    args = args.Add(SyntaxFactory.Argument(argExpression).WithTriviaFrom(ma));
                }
                else
                {
                    throw new NotImplementedException($"动态查询方法的第一个参数[{lambda.Body.GetType().Name}]暂未实现");
                }

                res = SyntaxFactory.ArgumentList(args);
            }
            else if (queryMethodCtx.Current.MethodName == TypeHelper.SqlQueryToScalarMethod)
            {
                var args = new SeparatedSyntaxList<ArgumentSyntax>();
                var argExpression = (ExpressionSyntax)lambda.Body.Accept(this);
                var arg = SyntaxFactory.Argument(argExpression);
                args = args.Add(arg);
                res = SyntaxFactory.ArgumentList(args);
            }
            else
            {
                res = Visit(lambda.Body);
            }

            queryMethodCtx.Current.InLambdaExpression = false;
            return res;
        }
        #endregion

        #region ====Declaration====

        #region ----ClassDeclaration----
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            //只处理服务类型
            if (TypeHelper.IsServiceClass(node, AppName, ServiceModel.Name))
            {
                //先加入IService接口
                var updatedNode = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
                updatedNode = updatedNode.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.ServiceInterfaceType));
                //再加入IService接口实现
                var invokeMethod = (MethodDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(GenerateIServiceImplementsCode()).Members[0];
                return updatedNode.AddMembers(invokeMethod);
            }
            else
            {
                return base.VisitClassDeclaration(node);
            }
        }
        #endregion

        #region ----MethodDeclaration----
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var updateNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            //处理返回类型
            if (node.ReturnType.ToString() != "void")
            {
                var returnTypeSymbol = SemanticModel.GetSymbolInfo(node.ReturnType).Symbol;
                var returnRuntimeType = TypeHelper.ConvertToRuntimeType(returnTypeSymbol);
                if (returnRuntimeType != null)
                    updateNode = updateNode.WithReturnType(returnRuntimeType);
            }

            //判断方法是否属于服务类且标为公开
            if (TypeHelper.IsServiceClass(node.Parent as ClassDeclarationSyntax, AppName, ServiceModel.Name))
            {
                if (TypeHelper.IsServiceMethod(node)) //处理公开的服务方法，加入列表
                {
                    publicMethods.Add(updateNode);
                }
            }

            return updateNode;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            if (node.Type != null)
            {
                var symbol = SemanticModel.GetSymbolInfo(node.Type).Symbol;
                var runtimeType = TypeHelper.ConvertToRuntimeType(symbol);
                if (runtimeType != null)
                    return node.WithType(runtimeType);
            }

            return base.VisitParameter(node);
        }
        #endregion

        #region ----VariableDeclaration----
        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            //忽略var声明
            var varIdentifier = node.Type as IdentifierNameSyntax;
            if (varIdentifier != null && varIdentifier.Identifier.ValueText == "var")
                return base.VisitVariableDeclaration(node);

            var typeSymbol = SemanticModel.GetSymbolInfo(node.Type).Symbol;
            if (typeSymbol != null)
            {
                var runtimeType = TypeHelper.ConvertToRuntimeType(typeSymbol);
                if (runtimeType != null)
                {
                    var updatedNode = (VariableDeclarationSyntax)base.VisitVariableDeclaration(node);
                    return updatedNode.WithType(runtimeType).WithTriviaFrom(node);
                }
            }

            return base.VisitVariableDeclaration(node);
        }
        #endregion

        #endregion

        #region ====Helpers====
        /// <summary>
        /// 根据实体成员Symbol获取EntityMemberId
        /// </summary>
        internal ushort GetEntityMemberId(ISymbol symbol)
        {
            var names = symbol.ContainingType.ToString().Split('.');
            var appNode = hub.DesignTree.FindApplicationNodeByName(names[0]);
            var entityModelNode = hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
            var entityModel = (EntityModel)entityModelNode.Model;
            return entityModel.GetMember(symbol.Name, true).MemberId;
        }

        /// <summary>
        /// 生成实现IService的代码
        /// </summary>
        private string GenerateIServiceImplementsCode()
        {
            var sb = new StringBuilder("public async System.Threading.Tasks.ValueTask<appbox.Data.AnyValue> InvokeAsync(System.ReadOnlyMemory<char> method, appbox.Data.InvokeArgs args) { switch(method) { ");
            foreach (var method in publicMethods)
            {
                sb.AppendFormat("case System.ReadOnlyMemory<char> s when s.Span.SequenceEqual(\"{0}\"):", method.Identifier.ValueText);
                //插入验证权限代码
                if (publicMethodsInvokePermissions.TryGetValue(method.Identifier.ValueText, out string invokePermissionCode))
                {
                    sb.AppendFormat("{1}if (!({0})) throw new System.Exception(\"无调用服务方法的权限\");{1}",
                                    invokePermissionCode, Environment.NewLine);
                }
                //插入调用代码
                //TODO:暂简单判断有无返回值，应直接判断是否Awaitable，另处理同步方法调用
                var returnTypeSpan = method.ReturnType.ToString().AsSpan();
                bool isReturnTask = returnTypeSpan.Contains("Task<", StringComparison.Ordinal)
                                    || returnTypeSpan.Contains("ValueTask<", StringComparison.Ordinal);
                if (isReturnTask)
                    sb.Append("return appbox.Data.AnyValue.From(");
                sb.Append("await ");
                sb.AppendFormat("{0}(", method.Identifier.ValueText);
                for (int i = 0; i < method.ParameterList.Parameters.Count; i++)
                {
                    //var typeSymbol = SemanticModel.GetSymbolInfo(method.ParameterList.Parameters[i].Type).Symbol;
                    //if (IsEntityClass(typeSymbol as INamedTypeSymbol))
                    //    sb.Append("(AppBox.Core.Entity)args[" + i.ToString() + "]");
                    //else
                    //sb.AppendFormat("({0})args[{1}]", paraType, i);
                    var paraType = method.ParameterList.Parameters[i].Type.ToString();
                    sb.Append(GenArgsGetMethod(paraType));

                    if (i != method.ParameterList.Parameters.Count - 1)
                        sb.Append(",");
                }
                if (isReturnTask)
                    sb.Append("));");
                else
                    sb.Append("); return appbox.Data.AnyValue.Empty;");
            }
            sb.Append("default: throw new System.Exception(\"Cannot find method: \" + method); }}");
            return sb.ToString();
        }

        /// <summary>
        /// 生成IService调用时根据参数类型生成如args.GetString()
        /// </summary>
        private static string GenArgsGetMethod(string argType)
        {
            switch (argType) //TODO: fix other types
            {
                case "bool": return "args.GetBoolean()";
                case "int": return "args.GetInt32()";
                case "float": return "args.GetFloat()";
                case "double": return "args.GetDouble()";
                case "char": return "args.GetChar()";
                case "sbyte": return "args.GetSByte()";
                case "byte": return "args.GetByte()";
                case "string": return "args.GetString()";
                case "DateTime":
                case "System.DateTime": return "args.GetDateTime()";
                case "Guid":
                case "System.Guid": return "args.GetGuid()";
                default:
                    return $"({argType})args.GetObject()";
            }
        }

        // 从q.OrderBy(t => t.Name).OrderBy(o => o.Code)获取q
        private static IdentifierNameSyntax GetIdentifier(ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invoke)
                return GetIdentifier(invoke.Expression);
            if (expression is MemberAccessExpressionSyntax memberAccess)
                return GetIdentifier(memberAccess.Expression);
            if (expression is IdentifierNameSyntax identifier)
                return identifier;
            throw new NotSupportedException($"expression type: {expression.GetType().Name}");
        }
        #endregion
    }

}
