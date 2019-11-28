using System;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将TableScan.ToTreeListAsync(t => t.EntitySet)转换为ToTreeListAsync(ushort setMemberId)
    /// </summary>
    sealed class ToTreeListInterceptor : IInvocationInterceptor<SyntaxNode>
    {

        internal const string Name = "ToTreeList";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            var target = node.ArgumentList.Arguments[0].Expression;
            MemberAccessExpressionSyntax memberAccess = null;
            if (target is SimpleLambdaExpressionSyntax)
                memberAccess = ((SimpleLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;
            else if (target is ParenthesizedLambdaExpressionSyntax)
                memberAccess = ((ParenthesizedLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;

            if (memberAccess == null)
                throw new ArgumentException("ToTreeListAsync参数错误");

            //TODO:判断只允许t.EntitySet，其他如t.EntityRef.EntitySet不允许

            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var expSymbol = generator.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            var memberId = generator.GetEntityMemberId(expSymbol);
            var arg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression
                                            , SyntaxFactory.Literal(memberId)));
            var argList = SyntaxFactory.ArgumentList().AddArguments(arg);
            return node.WithArgumentList(argList);
        }
    }
}
