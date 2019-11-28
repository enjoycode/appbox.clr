using System;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    sealed class LoadEntitySetInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "LoadEntitySet";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            //先将第一个范型参数转换为模型Id，忽略第二个范型参数
            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var appName = symbol.TypeArguments[0].ContainingNamespace.ContainingNamespace.Name;
            var modelTypeName = symbol.TypeArguments[0].Name;
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(appName);
            var modelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, modelTypeName);

            var exp = SyntaxFactory.ParseExpression("appbox.Store.EntityStore.LoadEntitySetAsync");

            //第一个参数
            var modelIdArg = SyntaxFactory.Argument(
                 SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(modelNode.Model.Id)));
            var args = SyntaxFactory.ArgumentList().AddArguments(modelIdArg);
            //第二个参数
            args = args.AddArguments((ArgumentSyntax)visitor.Visit(node.ArgumentList.Arguments[0]));
            //第三个参数
            var target = node.ArgumentList.Arguments[1].Expression;
            MemberAccessExpressionSyntax memberAccess = null;
            if (target is SimpleLambdaExpressionSyntax)
                memberAccess = ((SimpleLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;
            else if (target is ParenthesizedLambdaExpressionSyntax)
                memberAccess = ((ParenthesizedLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;

            if (memberAccess == null)
                throw new ArgumentException("LoadEntitySetAsync参数错误");
            var expSymbol = generator.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            var memberId = generator.GetEntityMemberId(expSymbol);
            var arg3 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression
                                            , SyntaxFactory.Literal(memberId)));
            args = args.AddArguments(arg3);

            var res = SyntaxFactory.InvocationExpression(exp, args).WithTriviaFrom(node);
            return res;
        }
    }
}
