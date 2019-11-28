using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design
{
    static class SyntaxNodeExtensions
    {
        public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                // Because the syntax for conditional access is right associate, we cannot
                // simply take the first ancestor ConditionalAccessExpression. Instead, we 
                // must walk upward until we find the ConditionalAccessExpression whose
                // OperatorToken appears left of the MemberBinding.
                if (parent.IsKind(SyntaxKind.ConditionalAccessExpression) &&
                    ((ConditionalAccessExpressionSyntax)parent).OperatorToken.Span.End <= node.SpanStart)
                {
                    return (ConditionalAccessExpressionSyntax)parent;
                }

                parent = parent.Parent;
            }

            return null;
        }

        public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }
    }
}
