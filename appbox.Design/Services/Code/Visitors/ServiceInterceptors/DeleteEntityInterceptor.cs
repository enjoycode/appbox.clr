using System;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将EntityStore.DeleteAsync/<Entity/>(id)转换为EntityStore.DeleteAsync(modelId, id)
    /// </summary>
    sealed class DeleteEntityInterceptor : IInvocationInterceptor<SyntaxNode>
    {

        internal const string Name = "DeleteEntity";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            //先将范型参数转换为模型Id
            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var appName = symbol.TypeArguments[0].ContainingNamespace.ContainingNamespace.Name;
            var modelTypeName = symbol.TypeArguments[0].Name;
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(appName);
            var modelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, modelTypeName);

            var exp = SyntaxFactory.ParseExpression("appbox.Store.EntityStore.DeleteAsync");

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
