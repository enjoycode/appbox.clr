using System;
using System.Text;
using appbox.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 转换类似CqlQuery.ToEntityList(t => t.Amount > 0) 
    /// 至 .ToDataTable(t => t.GetValue<int>("Amount") > 0)
    /// </summary>
    sealed class CqlToEntityListInterceptor : IInvocationInterceptor<SyntaxNode>
    {

        internal const string Name = "CqlToEntityList";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol,
            CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            var serviceCodeGenerator = (ServiceCodeGenerator)visitor;
            var sb = StringBuilderCache.Acquire();
            sb.Append(node.Expression);
            sb.Append('(');

            //处理参数
            for (int i = 0; i < node.ArgumentList.Arguments.Count; i++)
            {
                if (i == 0)
                {
                    string lambdaParamter;
                    CSharpSyntaxNode lambdaBody;
                    var firstArg = node.ArgumentList.Arguments[i];
                    if (firstArg.Expression is SimpleLambdaExpressionSyntax)
                    {
                        var lambda = (SimpleLambdaExpressionSyntax)firstArg.Expression;
                        lambdaParamter = lambda.Parameter.Identifier.ValueText;
                        lambdaBody = lambda.Body;
                    }
                    else if (firstArg.Expression is ParenthesizedLambdaExpressionSyntax)
                    {
                        var lambda = (ParenthesizedLambdaExpressionSyntax)firstArg.Expression;
                        if (lambda.ParameterList.Parameters.Count != 1)
                            throw new ArgumentException("CqlToListInterceptor");
                        lambdaParamter = lambda.ParameterList.Parameters[0].Identifier.ValueText;
                        lambdaBody = lambda.Body;
                    }
                    else
                    {
                        throw new ArgumentException("CqlToListInterceptor: first argument must be Lambda");
                    }

                    sb.Append(lambdaParamter);
                    sb.Append(" => ");
                    VisitFilterLambdaExpression(lambdaBody, sb, lambdaParamter, serviceCodeGenerator);
                }
                else
                {
                    sb.Append(',');
                    sb.Append(node.ArgumentList.Arguments[i].Accept(visitor));
                }
            }
            sb.Append(')');

            var newInvocation = SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb));
            //检测并消除行差
            var location = node.GetLocation();
            var pos = location.GetLineSpan();
            var lineDiff = pos.EndLinePosition.Line - pos.StartLinePosition.Line;
            if (lineDiff > 0)
                return newInvocation.WithTriviaFrom(node).WithTrailingTrivia(SyntaxFactory.Whitespace(new string('\n', lineDiff)));
            else
                return newInvocation.WithTriviaFrom(node);
        }

        private static void VisitFilterLambdaExpression(CSharpSyntaxNode body, StringBuilder sb,
            string lambdaParamter, ServiceCodeGenerator visitor)
        {
            visitor.cqlFilterLambdaParameter = lambdaParamter;
            sb.Append(body.Accept(visitor));
            visitor.cqlFilterLambdaParameter = null;
        }

    }
}
