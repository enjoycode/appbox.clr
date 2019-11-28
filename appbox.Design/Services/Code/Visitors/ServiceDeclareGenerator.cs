using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using appbox.Models;
using appbox.Caching;
namespace appbox.Design
{
    /// <summary>
    /// 用于生成前端TypeScript的声明文件
    /// </summary>
    sealed class ServiceDeclareGenerator : CSharpSyntaxWalker
    {

        public SemanticModel SemanticModel { get; private set; }

        public ServiceModel ServiceModel { get; private set; }

        public string AppName { get; private set; }

        private readonly DesignHub hub;
        private StringBuilder sb;
        private bool firstParameter;

        public ServiceDeclareGenerator(DesignHub hub, string appName, SemanticModel semanticModel, ServiceModel serviceModel)
        {
            this.hub = hub;
            AppName = appName;
            SemanticModel = semanticModel;
            ServiceModel = serviceModel;
        }

        public string GetDeclare()
        {
            if (sb == null)
                return null;
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (TypeHelper.IsServiceClass(node, AppName, ServiceModel.Name))
            {
                sb = StringBuilderCache.Acquire();
                sb.Append($"declare namespace {AppName}.Services.{ServiceModel.Name} {{");
                base.VisitClassDeclaration(node);
                sb.Append('}');
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (TypeHelper.IsServiceMethod(node))
            {
                sb.Append($"function {node.Identifier.Text}(");
                //处理参数列表
                firstParameter = true;
                Visit(node.ParameterList);
                sb.Append(')');
                //处理返回类型
                sb.Append(":Promise");
                if (node.ReturnType.ToString() != "void")
                {
                    var symbol = SemanticModel.GetSymbolInfo(node.ReturnType).Symbol;
                    var typeSymbol = symbol as INamedTypeSymbol;
                    if (TypeHelper.IsTaskOrValueTask(symbol) && typeSymbol.IsGenericType)
                    {
                        if (typeSymbol.TypeArguments.Length == 1)
                        {
                            sb.Append('<');
                            sb.Append(ConvertToScriptType(typeSymbol.TypeArguments[0]));
                            sb.Append('>');
                        }
                        else
                        {
                            sb.Append("<any>"); //无法处理多个范型参数
                        }
                    }
                    else
                    {
                        sb.Append('<');
                        sb.Append(ConvertToScriptType(symbol));
                        sb.Append('>');
                    }
                }
                else
                {
                    sb.Append("<void>");
                }
                sb.Append(';');
            }
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            if (firstParameter)
                firstParameter = false;
            else
                sb.Append(',');

            sb.Append(node.Identifier.Value);
            sb.Append(':');
            var symbol = SemanticModel.GetSymbolInfo(node.Type).Symbol;
            sb.Append(ConvertToScriptType(symbol));
        }

        private static string ConvertToScriptType(ISymbol symbol)
        {
            //TODO: finish it, Entity, Enum
            if (symbol.IsArrayType())
            {
                var arrayType = (IArrayTypeSymbol)symbol;
                return ConvertToScriptType(arrayType.ElementType) + "[]";
            }

            var typeSymbol = symbol as INamedTypeSymbol;
            var interfaces = typeSymbol.Interfaces;
            var collection = interfaces.FirstOrDefault(t => t.ToString() == "System.Collections.ICollection");
            if (collection != null)
            {
                if (typeSymbol.IsGenericType && typeSymbol.TypeArguments.Length == 1) //TODO:Dictionary
                    return ConvertToScriptType(typeSymbol.TypeArguments[0]) + "[]";
                return "any[]";
            }

            var typeString = typeSymbol.ToString();
            switch (typeString)
            {
                case "System.Guid":
                case "string": return "string";

                case "int":
                case "uint":
                case "float":
                case "double":
                case "short":
                case "ushort":
                case "long":
                case "ulong":
                case "System.Decimal":
                case "byte": return "number";

                case "System.DateTime": return "Date";
            }

            return "any";
        }
    }
}
