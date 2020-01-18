using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    static class CqlLambdaHelper
    {
        internal static void BuildCqlLambdaGetValue(StringBuilder sb, string lambdaParamter,
            int memberIndex, string memberName,
            ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var memberSymbol = semanticModel.GetSymbolInfo(expression).Symbol;
            var memberType = TypeHelper.GetSymbolType(memberSymbol);
            var valueTypeString = memberType.ToString();

            if (valueTypeString.StartsWith("sys.Data.FieldSet<")) //FieldSet<T>转换
            {
                var elementType = memberType.GetTypeArguments()[0].ToString();
                sb.Append("new AppBox.Core.FieldSet<");
                sb.Append(elementType);
                sb.Append(">(");

                sb.Append(lambdaParamter);
                sb.Append(".GetValue<");
                sb.Append(elementType); //这里转换为获取数组值
                sb.Append("[]>(");
                if (memberIndex >= 0)
                {
                    sb.Append(memberIndex);
                }
                else
                {
                    sb.Append('\"');
                    sb.Append(memberName);
                    sb.Append('\"');
                }
                sb.Append(')');

                sb.Append(')');
            }
            else
            {
                sb.Append(lambdaParamter);
                sb.Append(".GetValue<");
                sb.Append(valueTypeString); //TODO:***暂简单处理
                sb.Append(">(");
                if (memberIndex >= 0)
                {
                    sb.Append(memberIndex);
                }
                else
                {
                    sb.Append('\"');
                    sb.Append(memberName);
                    sb.Append('\"');
                }
                sb.Append(')');
            }
        }
    }
}
