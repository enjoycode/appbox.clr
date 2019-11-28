﻿using System;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将EntityStore.LoadAsync/<Entity/>(id)转换为EntityStore.LoadAsync(modelId, id)
    /// </summary>
    sealed class LoadEntityInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "LoadEntity";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            //先将范型参数转换为模型Id
            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var appName = symbol.TypeArguments[0].ContainingNamespace.ContainingNamespace.Name;
            var modelTypeName = symbol.TypeArguments[0].Name;
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(appName);
            var modelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, modelTypeName);

            var exp = SyntaxFactory.ParseExpression("appbox.Store.EntityStore.LoadAsync");

            var modelIdArg = SyntaxFactory.Argument(
                 SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(modelNode.Model.Id)));
            var args = SyntaxFactory.ArgumentList().AddArguments(modelIdArg);
            foreach (var oldArg in node.ArgumentList.Arguments)
            {
                args = args.AddArguments((ArgumentSyntax)visitor.Visit(oldArg));
            }

            var res = SyntaxFactory.InvocationExpression(exp, args).WithTriviaFrom(node);
            return res;
        }
    }
}
