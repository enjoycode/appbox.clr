using System;
using Microsoft.CodeAnalysis;

namespace appbox.Design
{
    static class TypeExtensions
    {
        /// <summary>
        /// Gets the full name. The full name is no 1:1 representation of a type it's missing generics and it has a poor
        /// representation for inner types (just dot separated).
        /// DO NOT use this method unless you're know what you do. It's only implemented for legacy code.
        /// </summary>
        public static string GetFullName(this ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }
    }
}
