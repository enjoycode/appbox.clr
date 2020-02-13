using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 转换服务调用服务的代码，eg:
    /// var res = await sys.Services.HelloService.SayHello("aa")
    /// var res = await appbox.Runtime.RuntimeContext.InvokeAsync<string>(InvokeArgs.From(AnyValue.From("aa")))
    /// </summary>
    sealed class CallServiceInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "CallService";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node,
            IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            //将旧方法转换为服务名称参数
            var service = $"{symbol.ContainingType.ContainingNamespace.ContainingNamespace}.{symbol.ContainingType.Name}.{symbol.Name}";

            var returnType = symbol.ReturnType as INamedTypeSymbol;
            var method = SyntaxFactory.ParseExpression(GetMethodByReturnType(returnType))
                .WithTriviaFrom(node.Expression);

            //添加服务名称参数
            var serviceArg = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(service)));
            var args = SyntaxFactory.ArgumentList().AddArguments(serviceArg);
            //转换旧有参数（如果有）
            args = MakeInvokeArgs(node, visitor, args).WithTriviaFrom(node.ArgumentList);

            var res = SyntaxFactory.InvocationExpression(method, args).WithTriviaFrom(node);
            return res;
        }

        private static string GetMethodByReturnType(INamedTypeSymbol returnType)
        {
            if (!returnType.IsGenericType) //无返回类型
                return "appbox.Runtime.RuntimeContext.Current.InvokeAsync";

            //有返回类型需要转换为对应的运行时方法
            var typeName = returnType.TypeArguments[0].ToString();
            return typeName switch
            {
                "bool" => "appbox.Runtime.RuntimeContext.InvokeBooleanAsync",
                "byte" => "appbox.Runtime.RuntimeContext.InvokeByteAsync",
                "ushort" => "appbox.Runtime.RuntimeContext.InvokeUInt16Async",
                "short" => "appbox.Runtime.RuntimeContext.InvokeInt16Async",
                "uint" => "appbox.Runtime.RuntimeContext.InvokeUInt32Async",
                "int" => "appbox.Runtime.RuntimeContext.InvokeInt32Async",
                "ulong" => "appbox.Runtime.RuntimeContext.InvokeUInt64Async",
                "long" => "appbox.Runtime.RuntimeContext.InvokeInt64Async",
                "float" => "appbox.Runtime.RuntimeContext.InvokeFloatAsync",
                "double" => "appbox.Runtime.RuntimeContext.InvokeDoubleAsync",
                "System.DateTime" => "appbox.Runtime.RuntimeContext.InvokeDateTimeAsync",
                "System.Guid" => "appbox.Runtime.RuntimeContext.InvokeGuidAsync",
                "decimal" => "appbox.Runtime.RuntimeContext.InvokeDecimalAsync",
                _ => $"appbox.Runtime.RuntimeContext.InvokeAsync<{typeName}>",
            };
        }

        private static ArgumentListSyntax MakeInvokeArgs(InvocationExpressionSyntax node,
            CSharpSyntaxVisitor<SyntaxNode> visitor, ArgumentListSyntax srcArgs)
        {
            if (node.ArgumentList.Arguments.Count == 0) return srcArgs;

            var argsFromMethod = SyntaxFactory.ParseExpression("appbox.Data.InvokeArgs.From");
            var args = SyntaxFactory.ArgumentList();
            foreach (var oldArg in node.ArgumentList.Arguments)
            {
                var anyValueFromMethod = SyntaxFactory.ParseExpression("appbox.Data.AnyValue.From");
                var valueArgs = SyntaxFactory.ArgumentList().AddArguments(
                        SyntaxFactory.Argument((ExpressionSyntax)visitor.Visit(oldArg.Expression))
                    );
                var newArg = SyntaxFactory.Argument(
                        SyntaxFactory.InvocationExpression(anyValueFromMethod, valueArgs)
                    ).WithTriviaFrom(oldArg);
                args = args.AddArguments(newArg);
            }

            return srcArgs.AddArguments(
                    SyntaxFactory.Argument(
                            SyntaxFactory.InvocationExpression(argsFromMethod, args)
                        )
                );
        }
    }
}
