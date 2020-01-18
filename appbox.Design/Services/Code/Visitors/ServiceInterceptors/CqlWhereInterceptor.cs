using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 转换类似CqlQuery.Equals(t => t.ID, 1) 至 CqlQuery.Where("\"ID\"=1")
    /// </summary>
    sealed class CqlWhereInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "CqlWhere";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node,
            IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            // t => t.Name
            if (!(node.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax firstArgExp))
                throw new Exception("FirstArgExp error");
            if (!(firstArgExp.Body is MemberAccessExpressionSyntax memberAccess))
                throw new Exception("MemberAccessExp error");
            var op = symbol.Name switch
            {
                "Equals" => "=",
                "GreaterThan" => ">",
                "GreaterThanOrEqual" => ">=",
                "LessThan" => "<",
                "LessThanOrEqual" => "<=",
                "In" => "IN",
                _ => throw new NotImplementedException(symbol.Name),
            };

            // value
            var valueArgExp = node.ArgumentList.Arguments[1].Expression;
            string expression = null;
            if (op == "IN") //注意：IN操作单独处理
            {
                expression = valueArgExp.Accept(visitor).ToString();
                var updated = node.Expression.Accept(visitor);
                var newInvocation = SyntaxFactory.ParseExpression($"{updated}(\"\\\"{memberAccess.Name}\\\"\", {expression})");
                return newInvocation.WithTriviaFrom(node);
            }
            else
            {
                var literal = valueArgExp as LiteralExpressionSyntax;
                var typeString = symbol.Parameters[1].Type.ToString();
                if (literal != null) //字面量直接转换，注意：字面量不可能类型为DateTime
                {
                    if (typeString == "string" || typeString == "System.String")
                        expression = $"'{literal.Token.ValueText}'";
                    else
                        expression = literal.ToString();
                    expression = $"\"\\\"{memberAccess.Name}\\\" {op} {expression}\"";
                }
                else //其他表达式
                {
                    expression = valueArgExp.Accept(visitor).ToString();
                    if (typeString.StartsWith("System.DateTime"))
                        expression = $"(long)(({expression} - new DateTime(1970, 1, 1)).TotalMilliseconds)";

                    if (typeString == "string" || typeString == "System.String")
                        expression = $"'{{{expression}}}'";
                    else
                        expression = $"{{{expression}}}";
                    expression = $"$\"\\\"{memberAccess.Name}\\\" {op} {expression}\"";
                }

                var updated = ((MemberAccessExpressionSyntax)node.Expression).Expression.Accept(visitor);
                var newInvocation = SyntaxFactory.ParseExpression(updated.ToString() + ".Where(" + expression + ")");
                return newInvocation.WithTriviaFrom(node);
            }

            // var newNode = node.WithTrailingTrivia(SyntaxFactory.Whitespace("\n\n\n"));
            // var newNode2 = node.Parent.WithTrailingTrivia(SyntaxFactory.Whitespace("\n\n\n"));
            // var location1 = node.GetLocation();
            // var pos1 = location1.GetLineSpan();
        }
    }
}
