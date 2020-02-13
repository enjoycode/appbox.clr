using System;
using System.Collections.Generic;
using appbox.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design
{
    static class TypeHelper
    {

        internal const string SqlQueryToDataTableMethod = "ToDataTable";
        internal const string SqlQueryToListMethod = "ToListAsync";
        internal const string SqlQueryToScalarMethod = "ToScalar";

        #region ----设计时类型常量----
        internal const string MemberAccessInterceptorAttribute = "System.Reflection.MemberAccessInterceptorAttribute";
        internal const string InvocationInterceptorAttribute = "System.Reflection.InvocationInterceptorAttribute";

        internal const string GenericCreateAttribute = "System.Reflection.GenericCreateAttribute";
        internal const string QueriableClassAttribute = "System.Reflection.QueriableClassAttribute";
        internal const string QueryMethodAttribute = "System.Reflection.QueryMethodAttribute";
        internal const string EnumModelAttribute = "System.Reflection.EnumModelAttribute";
        internal const string ResourceModelAttribute = "System.Reflection.ResourceModelAttribute";
        internal const string RealTypeAttribute = "System.Reflection.RealTypeAttribute";
        internal const string InvokePermissionAttribute = "sys.InvokePermissionAttribute";

        internal const string Type_EntityBase = "sys.EntityBase";
        internal const string Type_SysEntityBase = "sys.SysEntityBase";
        internal const string Type_EntityList = "sys.EntityList";
        internal const string Type_FieldSet = "sys.Data.FieldSet";
        //internal const string Type_IImageSource = "sys.IImageSource";

        internal const string Type_SqlStore = "SqlStore";
        internal const string Type_CqlStore = "CqlStore";
        //internal const string Type_BlobStore = "sys.Data.BlobStore";
        #endregion

        #region ----运行时类型常量----
        internal const string RuntimeType_Entity = "appbox.Data.Entity";
        internal const string RuntimeType_EntityList = "appbox.Data.EntityList";
        internal const string RuntimeType_IImageSource = "appbox.Data.IImageSource";
        #endregion

        #region ====IsXXX Methods====
        internal static bool IsTaskOrValueTask(ISymbol symbol)
        {
            //TODO:暂简单判断
            var span = symbol.ToString().AsSpan();
            if (span.StartsWith("System.Threading.Tasks.Task")
                || span.StartsWith("System.Threading.Tasks.ValueTask"))
                return true;
            return false;
        }

        internal static bool IsEntityClass(INamedTypeSymbol typeSymbol)
        {
            for (; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
            {
                if (typeSymbol.ToString() == Type_EntityBase)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsServiceClass(ClassDeclarationSyntax node, string appName, string serviceName)
        {
            if (node == null)
                return false;

            return node.Parent is NamespaceDeclarationSyntax parentNamespace
                && parentNamespace.Name.ToString() == appName + ".ServiceLogic"
                && node.Identifier.ValueText == serviceName;
        }

        internal static bool IsServiceMethod(MethodDeclarationSyntax node)
        {
            if (node == null)
                return false;

            //TODO:暂简单判断方法是否public，还需要判断返回类型
            bool foundPublicModifier = false;
            for (int i = 0; i < node.Modifiers.Count; i++)
            {
                if (node.Modifiers[i].ValueText == "public")
                {
                    foundPublicModifier = true;
                    break;
                }
            }
            return foundPublicModifier;
        }

        internal static bool IsWorkflowClass(INamedTypeSymbol typeSymbol)
        {
            for (; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
            {
                if (typeSymbol.ToString() == "wf.WorkflowInstance")
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFormClass(INamedTypeSymbol typeSymbol)
        {
            for (; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
            {
                if (typeSymbol.ToString() == "ui.RootView")
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSystemFuncsClass(ITypeSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
                return typeSymbol.ToString() == "sys.Funcs";
            }
            return false;
        }

        internal static bool IsGuiFuncMethod(IMethodSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
                var attributes = typeSymbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == "ui.GuiFuncAttribute")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsEnumModel(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
                var attributes = typeSymbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == EnumModelAttribute)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsGenericCreateClass(ITypeSymbol typeSymbol, ref AttributeData realTypeAttribute)
        {
            if (typeSymbol != null)
            {
                var attributes = typeSymbol.GetAttributes();
                bool found = false;
                AttributeData realTypeAttr = null;

                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == GenericCreateAttribute)
                    {
                        found = true;
                        if (realTypeAttr != null)
                            break;
                    }
                    else if (item.AttributeClass.ToString() == RealTypeAttribute)
                    {
                        realTypeAttr = item;
                        if (found)
                            break;
                    }
                }

                if (found && realTypeAttr != null)
                {
                    realTypeAttribute = realTypeAttr;
                    return true;
                }
            }
            return false;
        }

        internal static bool IsQuerialbeClass(INamedTypeSymbol typeSymbol, out bool isSystemQuery)
        {
            if (typeSymbol != null)
            {
                var attributes = typeSymbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == QueriableClassAttribute)
                    {
                        isSystemQuery = (bool)item.ConstructorArguments[0].Value;
                        return true;
                    }
                }
            }
            isSystemQuery = false;
            return false;
        }

        internal static bool IsQueryMethod(IMethodSymbol methodSymbol)
        {
            if (methodSymbol != null)
            {
                var attributes = methodSymbol.GetAttributes();
                foreach (var item in attributes)
                {
                    if (item.AttributeClass.ToString() == QueryMethodAttribute)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static string IsDataStoreClass(ITypeSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
                var typeString = typeSymbol.ToString();
                if (typeString == Type_SqlStore || typeString == Type_CqlStore)
                    return typeString;
                //if (typeString == Type_BlobStore)
                //    return "BlobStore";
            }
            return null;
        }
        #endregion

        #region ====运行时类型转换====
        private static readonly Dictionary<string, TypeSyntax> realTypes = new Dictionary<string, TypeSyntax>();

        /// <summary>
        /// 转换特殊虚拟类型至运行时类型，目前用于转换方法定义的输入参数类型及返回类型
        /// eg: sys.Entities.OrgUnit -> appbox.Data.Entity
        /// </summary>
        /// <returns>
        /// 返回null表示不用转换
        /// </returns>
        internal static TypeSyntax ConvertToRuntimeType(ISymbol symbol)
        {
            string realTypeName;
            if (symbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                var elementRealType = ConvertToRuntimeType(arrayTypeSymbol.ElementType);
                return elementRealType == null ? null : SyntaxFactory.ParseTypeName($"{elementRealType.ToString()}[]");
            }

            var typeSymbol = symbol as INamedTypeSymbol;
            if (IsEntityClass(typeSymbol))
            {
                return EntityTypeSyntax;
            }
            else if (IsEnumModel(typeSymbol))
            {
                return SyntaxFactory.ParseTypeName("int");
            }
            else
            {
                realTypeName = GetRealTypeName(symbol);
                if (realTypeName != null
                    && (!typeSymbol.IsGenericType || realTypeName == RuntimeType_EntityList)) //TODO:暂简单排除EntityList
                    return GetRealType(realTypeName);
            }

            if (typeSymbol != null && typeSymbol.IsGenericType) //范型特殊处理
            {
                bool needConvert = realTypeName != null;
                var sb = StringBuilderCache.Acquire();
                if (realTypeName != null) //之前已需要转换的类型
                    sb.Append(realTypeName);
                else
                    sb.AppendFormat("{0}.{1}", typeSymbol.ContainingSymbol, typeSymbol.Name);

                sb.Append('<');
                for (int i = 0; i < typeSymbol.TypeArguments.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    var argRealType = ConvertToRuntimeType(typeSymbol.TypeArguments[i]);
                    if (argRealType != null)
                    {
                        needConvert = true;
                        sb.Append(argRealType);
                    }
                    else
                    {
                        sb.Append(typeSymbol.TypeArguments[i]);
                    }
                }
                sb.Append('>');

                if (needConvert)
                    return SyntaxFactory.ParseTypeName(StringBuilderCache.GetStringAndRelease(sb));
            }

            return null;
        }

        internal static TypeSyntax GetRealType(string realTypeName)
        {
            if (!realTypes.TryGetValue(realTypeName, out TypeSyntax found))
            {
                found = SyntaxFactory.ParseTypeName(realTypeName);
                realTypes.Add(realTypeName, found);
            }
            return found;
        }

        internal static TypeSyntax EntityTypeSyntax
        {
            get { return GetRealType(RuntimeType_Entity); }
        }

        internal static TypeSyntax ServiceInterfaceType
        {
            get { return GetRealType("appbox.Runtime.IService"); }
        }

        internal static string GetRealTypeName(ISymbol typeSymbol)
        {
            if (typeSymbol == null) return null;

            var attributes = typeSymbol.GetAttributes();
            foreach (var item in attributes)
            {
                if (item.AttributeClass.ToString() == RealTypeAttribute)
                {
                    return (string)item.ConstructorArguments[0].Value;
                }
            }

            return null;
        }
        #endregion

        #region ====实体成员类型转换====
        internal static string GetEntityMemberTypeString(ITypeSymbol valueTypeSymbol, out bool isNullable)
        {
            isNullable = false;
            string valueTypeString = valueTypeSymbol.ToString();
            //先处理一些特殊类型
            if (valueTypeString.AsSpan().EndsWith("?")) //nullable
            {
                valueTypeString = valueTypeString.Remove(valueTypeString.Length - 1, 1);
                isNullable = true;
            }
            else if (valueTypeString.AsSpan().StartsWith(Type_EntityList)) //TODO:暂简单判断
                valueTypeString = RuntimeType_EntityList;
            else if (IsEntityClass(valueTypeSymbol as INamedTypeSymbol))
                valueTypeString = RuntimeType_Entity;

            string type;
            switch (valueTypeString)
            {
                //TODO: fix others
                case "short": type = "Int16"; break;
                case "int": type = "Int32"; break;
                case "long": type = "Int64"; break;
                case "ulong": type = "UInt64"; break;
                case "bool": type = "Boolean"; break;
                case "byte": type = "Byte"; break;
                case "float": type = "Float"; break;
                case "double": type = "Double"; break;
                case "decimal": type = "Decimal"; break;
                case "System.Guid":
                case "Guid": type = "Guid"; break;
                case "System.DateTime":
                case "DateTime": type = "DateTime"; break;
                case "byte[]": type = "Bytes"; break;
                case "string": type = "String"; break;
                case "sys.EntityId": type = "EntityId"; break;
                //case Type_IImageSource: type = "ImageSource"; break;
                case RuntimeType_Entity: type = "EntityRef"; break;
                case RuntimeType_EntityList: type = "EntitySet"; break;
                default: //other enum
                    type = "Int32"; break;
            }
            return type;
        }

        /// <summary>
        /// 用于VisitAssignment及VisitMemberAccess生成Entity.GetXXXValue及Entity.SetXXXValue
        /// </summary>
        internal static string GenEntityMemberGetterOrSetter(ITypeSymbol valueTypeSymbol, bool isGet)
        {
            //var valueTypeString = valueTypeSymbol.ToString();
            //if (valueTypeString.StartsWith(TypeHelper.Type_FieldSet)) //todo:优化判断
            //{
            //    return isGet ? valueTypeString.Replace(TypeHelper.Type_FieldSet, "GetFieldSetValue")
            //        : valueTypeString.Replace(TypeHelper.Type_FieldSet, "SetFieldSetValue");
            //}

            var type = GetEntityMemberTypeString(valueTypeSymbol, out bool isNullable);
            if (isNullable)
                return isGet ? $"Get{type}Nullable" : $"Set{type}Nullable";
            else
                return isGet ? $"Get{type}" : $"Set{type}";
        }
        #endregion

        internal static ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
                return ((IMethodSymbol)symbol).ReturnType;
            else if (symbol is ILocalSymbol)
                return ((ILocalSymbol)symbol).Type;
            else if (symbol is IParameterSymbol)
                return ((IParameterSymbol)symbol).Type;
            else if (symbol is IPropertySymbol)
                return ((IPropertySymbol)symbol).Type;
            else if (symbol is IFieldSymbol)
                return ((IFieldSymbol)symbol).Type;
            else
                return null;
        }

    }

}
