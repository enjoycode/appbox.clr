using System;
using System.Text;
using appbox.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 转换类似CqlQuery.ToList(t => new {Name=t.Customer, t.Amount }) 
    /// 至 .ToList(t => new {Name=t.GetValue<string>("Customer"), Amount=t.GetValue<int>("Amount")}, "\"Customer\" AS \"Name\", \"Amount\"" )
    /// </summary>
    sealed class CqlToListInterceptor : IInvocationInterceptor<SyntaxNode>
    {

        internal const string Name = "CqlToList";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            var serviceCodeGenerator = (ServiceCodeGenerator)visitor;
            var sb = StringBuilderCache.Acquire();
            sb.Append(node.Expression);
            sb.Append('(');
            //处理第一个selector Lambda参数
            string lambdaParamter;
            var firstArg = node.ArgumentList.Arguments[0];
            CSharpSyntaxNode lambdaBody;
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
                    throw new ArgumentException("CqlToListInvocationInterceptor");
                lambdaParamter = lambda.ParameterList.Parameters[0].Identifier.ValueText;
                lambdaBody = lambda.Body;
            }
            else
            {
                throw new ArgumentException("CqlToListInterceptor: first argument must be Lambda");
            }
            sb.Append(lambdaParamter);
            sb.Append(" => ");
            VisitSelectorLambdaExpresssion(lambdaBody, sb, lambdaParamter, serviceCodeGenerator);

            //处理后续参数
            for (int i = 1; i < node.ArgumentList.Arguments.Count; i++)
            {
                sb.Append(',');
                if (i == 1)
                {
                    var secondArg = node.ArgumentList.Arguments[i];
                    if (secondArg.Expression is SimpleLambdaExpressionSyntax)
                    {
                        var lambda = (SimpleLambdaExpressionSyntax)secondArg.Expression;
                        lambdaParamter = lambda.Parameter.Identifier.ValueText;
                        lambdaBody = lambda.Body;
                    }
                    else if (secondArg.Expression is ParenthesizedLambdaExpressionSyntax)
                    {
                        var lambda = (ParenthesizedLambdaExpressionSyntax)secondArg.Expression;
                        if (lambda.ParameterList.Parameters.Count != 1)
                            throw new ArgumentException("CqlToListInvocationInterceptor");
                        lambdaParamter = lambda.ParameterList.Parameters[0].Identifier.ValueText;
                        lambdaBody = lambda.Body;
                    }
                    else
                    {
                        throw new ArgumentException("CqlToListInvocationInterceptor:第一个参数必须是Lambda表达式");
                    }

                    sb.Append(lambdaParamter);
                    sb.Append(" => ");
                    VisitFilterLambdaExpression(lambdaBody, sb, lambdaParamter, serviceCodeGenerator);
                }
                else
                {
                    sb.Append(node.ArgumentList.Arguments[i].Accept(visitor));
                }
            }
            sb.Append(')');

            var newInvocation = SyntaxFactory.ParseExpression(StringBuilderCache.GetStringAndRelease(sb));
            //检测并消除行差
            var lineSpan = node.GetLocation().GetLineSpan();
            var lineDiff = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;
            if (lineDiff > 0)
                return newInvocation.WithTriviaFrom(node).WithTrailingTrivia(SyntaxFactory.Whitespace(new string('\n', lineDiff)));
            else
                return newInvocation.WithTriviaFrom(node);
        }

        private static void VisitSelectorLambdaExpresssion(CSharpSyntaxNode body, StringBuilder sb,
            string lambdaParamter, ServiceCodeGenerator visitor)
        {
            var aoc = body as AnonymousObjectCreationExpressionSyntax;
            if (aoc == null)
                throw new Exception("CqlToListInvocationInterceptor: 只支持AnonymousObjectCreation");

            sb.Append("new {");
            var vsb = StringBuilderCache.Acquire();
            AnonymousObjectMemberDeclaratorSyntax memberDeclarator;
            for (int i = 0; i < aoc.Initializers.Count; i++)
            {
                memberDeclarator = aoc.Initializers[i];
                var memberExpression = memberDeclarator.Expression as MemberAccessExpressionSyntax;
                if (memberExpression == null)
                    throw new Exception("CqlToListInvocationInterceptor: 只支持MemberAccessExpression");
                var memberName = memberExpression.Name.Identifier.ValueText;

                if (i != 0)
                {
                    sb.Append(',');
                    vsb.Append(',');
                }

                vsb.Append("\\\"");
                vsb.Append(memberName);
                vsb.Append("\\\"");
                if (memberDeclarator.NameEquals != null)
                {
                    var aliasName = memberDeclarator.NameEquals.Name.Identifier.ValueText;
                    vsb.Append(" AS \\\"");
                    vsb.Append(aliasName);
                    vsb.Append("\\\"");
                    memberName = aliasName;
                }

                sb.Append(memberName);
                sb.Append('=');
                CqlLambdaHelper.BuildCqlLambdaGetValue(sb, lambdaParamter, i, null, memberExpression, visitor.SemanticModel);
            }

            //附加第二个参数
            sb.Append("}, \"");
            sb.Append(StringBuilderCache.GetStringAndRelease(vsb));
            sb.Append("\"");
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
