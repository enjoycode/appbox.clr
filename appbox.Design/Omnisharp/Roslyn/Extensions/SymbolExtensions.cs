using System;
using Microsoft.CodeAnalysis;
using OmniSharp.Models;

namespace OmniSharp.Extensions
{
    public static class SymbolExtensions
    {
        public static string GetKind(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return Enum.GetName(namedType.TypeKind.GetType(), namedType.TypeKind);
            }

            if (symbol.Kind == SymbolKind.Field &&
                symbol.ContainingType?.TypeKind == TypeKind.Enum &&
                symbol.Name != WellKnownMemberNames.EnumBackingFieldName)
            {
                return "EnumMember";
            }

            if ((symbol as IFieldSymbol)?.IsConst == true)
            {
                return "Const";
            }

            return Enum.GetName(symbol.Kind.GetType(), symbol.Kind);
        }

        public static string GetAccessibilityString(this ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return SymbolAccessibilities.Public;
                case Accessibility.Internal:
                    return SymbolAccessibilities.Internal;
                case Accessibility.Private:
                    return SymbolAccessibilities.Private;
                case Accessibility.Protected:
                    return SymbolAccessibilities.Protected;
                case Accessibility.ProtectedOrInternal:
                    return SymbolAccessibilities.ProtectedInternal;
                case Accessibility.ProtectedAndInternal:
                    return SymbolAccessibilities.PrivateProtected;
                default:
                    return null;
            }
        }

        public static string GetKindString(this ISymbol symbol)
        {
            switch (symbol)
            {
                case INamespaceSymbol _:
                    return SymbolKinds.Namespace;
                case INamedTypeSymbol namedTypeSymbol:
                    return namedTypeSymbol.GetKindString();
                case IMethodSymbol methodSymbol:
                    return methodSymbol.GetKindString();
                case IFieldSymbol fieldSymbol:
                    return fieldSymbol.GetKindString();
                case IPropertySymbol propertySymbol:
                    return propertySymbol.GetKindString();
                case IEventSymbol _:
                    return SymbolKinds.Event;
                default:
                    return SymbolKinds.Unknown;
            }
        }

        public static string GetKindString(this INamedTypeSymbol namedTypeSymbol)
        {
            switch (namedTypeSymbol.TypeKind)
            {
                case TypeKind.Class:
                    return SymbolKinds.Class;
                case TypeKind.Delegate:
                    return SymbolKinds.Delegate;
                case TypeKind.Enum:
                    return SymbolKinds.Enum;
                case TypeKind.Interface:
                    return SymbolKinds.Interface;
                case TypeKind.Struct:
                    return SymbolKinds.Struct;
                default:
                    return SymbolKinds.Unknown;
            }
        }

        public static string GetKindString(this IMethodSymbol methodSymbol)
        {
            switch (methodSymbol.MethodKind)
            {
                case MethodKind.Ordinary:
                case MethodKind.ReducedExtension:
                case MethodKind.ExplicitInterfaceImplementation:
                    return SymbolKinds.Method;
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    return SymbolKinds.Constructor;
                case MethodKind.Destructor:
                    return SymbolKinds.Destructor;
                case MethodKind.Conversion:
                case MethodKind.BuiltinOperator:
                case MethodKind.UserDefinedOperator:
                    return SymbolKinds.Operator;
                default:
                    return SymbolKinds.Unknown;
            }
        }

        public static string GetKindString(this IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.ContainingType?.TypeKind == TypeKind.Enum &&
                fieldSymbol.HasConstantValue)
            {
                return SymbolKinds.EnumMember;
            }

            return fieldSymbol.IsConst
                ? SymbolKinds.Constant
                : SymbolKinds.Field;
        }

        public static string GetKindString(this IPropertySymbol propertySymbol)
        {
            return propertySymbol.IsIndexer
                ? SymbolKinds.Indexer
                : SymbolKinds.Property;
        }
    }
}
