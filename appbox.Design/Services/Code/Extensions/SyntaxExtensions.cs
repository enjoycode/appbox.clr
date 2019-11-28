using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace appbox.Design
{
    static class SyntaxExtensions
    {
        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node != null && node.Parent.IsKind(kind);
        }
    }
}
