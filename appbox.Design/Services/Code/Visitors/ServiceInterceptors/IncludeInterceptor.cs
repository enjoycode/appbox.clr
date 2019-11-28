using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using appbox.Models;

namespace appbox.Design.ServiceInterceptors
{
    sealed class IncludeInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "Include";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            var target = node.ArgumentList.Arguments[0].Expression;
            MemberAccessExpressionSyntax memberAccess = null;
            if (target is SimpleLambdaExpressionSyntax)
                memberAccess = ((SimpleLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;
            else if (target is ParenthesizedLambdaExpressionSyntax)
                memberAccess = ((ParenthesizedLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;

            if (memberAccess == null)
                throw new ArgumentException("Include参数错误");

            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            if (!(generator.SemanticModel.GetSymbolInfo(memberAccess).Symbol is IPropertySymbol memberSymbol))
                throw new ArgumentException("Include参数错误");

            if (TypeHelper.IsEntityClass(memberSymbol.Type as INamedTypeSymbol))
            {
                throw new NotImplementedException();
            }
            else if (memberSymbol.Type.ToString().StartsWith(TypeHelper.Type_EntityList, StringComparison.Ordinal)) //TODO:暂简单判断
            {
                throw new NotImplementedException();
            }
            else //Include(t => t.Customer.Name)
            {
                //先判断不允许t => t.Name
                if (memberAccess.Expression is IdentifierNameSyntax)
                    throw new Exception("不允许Include如t=>t.Name");

                var levels = new List<ValueTuple<string, ushort>>(); 
                PathToLevels(generator, memberAccess, memberSymbol, levels);
                levels.Reverse();
                var aliasName = string.Concat(levels.Select(t => t.Item1));

                var argsArray = new ArgumentSyntax[levels.Count + 1];
                argsArray[0] = SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(aliasName)));
                for (int i = 0; i < levels.Count; i++)
                {
                    argsArray[i + 1] = SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(levels[i].Item2)));
                }
                var args = SyntaxFactory.ArgumentList().AddArguments(argsArray);
                return node.WithArgumentList(args);
            }
        }

        private static void PathToLevels(ServiceCodeGenerator generator, MemberAccessExpressionSyntax memberAccess,
            IPropertySymbol memberSymbol, List<ValueTuple<string, ushort>> levels)
        {
            if (levels.Count > 3) throw new Exception("Include超出级数"); //TODO: 暂只支持3级 t.Customer.Region.Name
           
            var memberId = generator.GetEntityMemberId(memberSymbol);
            levels.Add(ValueTuple.Create(memberAccess.Name.Identifier.ValueText, memberId));
            //继续递归
            if (memberAccess.Expression is MemberAccessExpressionSyntax nextMemberAccess)
            {
                if (!(generator.SemanticModel.GetSymbolInfo(nextMemberAccess).Symbol is IPropertySymbol nextMemberSymbol))
                    throw new ArgumentException("Include参数错误");
                PathToLevels(generator, nextMemberAccess, nextMemberSymbol, levels);
            }
            else if (!(memberAccess.Expression is IdentifierNameSyntax))
            {
                throw new ArgumentException("Include参数错误");
            }
        }
    }
}
