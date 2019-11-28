using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace appbox.Design
{
    internal static class DataTipInfoGetter
    {
        internal static async Task<DebugDataTipInfo> GetInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root == null)
                {
                    return default(DebugDataTipInfo);
                }

                var token = root.FindToken(position);

                var expression = token.Parent as ExpressionSyntax;
                if (expression == null)
                {
                    return token.IsKind(SyntaxKind.IdentifierToken)
                        ? new DebugDataTipInfo(token.Span, text: null)
                        : default(DebugDataTipInfo);
                }

                if (expression.IsAnyLiteralExpression())
                {
                    // If the user hovers over a literal, give them a DataTip for the type of the
                    // literal they're hovering over.
                    // Partial semantics should always be sufficient because the (unconverted) type
                    // of a literal can always easily be determined.
                    var semanticModel = await document.GetSemanticModelAsync/*GetPartialSemanticModelAsync*/(cancellationToken).ConfigureAwait(false);
                    var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                    return type == null
                        ? default(DebugDataTipInfo)
                        : new DebugDataTipInfo(expression.Span, type.ToDisplayString() /*type.ToNameDisplayString()*/);
                }

                if (expression.IsRightSideOfDotOrArrow())
                {
                    var curr = expression;
                    while (true)
                    {
                        var conditionalAccess = curr.GetParentConditionalAccessExpression();
                        if (conditionalAccess == null)
                        {
                            break;
                        }

                        curr = conditionalAccess;
                    }

                    if (curr == expression)
                    {
                        // NB: Parent.Span, not Span as below.
                        return new DebugDataTipInfo(expression.Parent.Span, text: null);
                    }

                    // NOTE: There may not be an ExpressionSyntax corresponding to the range we want.
                    // For example, for input a?.$$B?.C, we want span [|a?.B|]?.C.
                    return new DebugDataTipInfo(TextSpan.FromBounds(curr.SpanStart, expression.Span.End), text: null);
                }

                // NOTE(cyrusn): This behavior is to mimic what we did in Dev10, I'm not sure if it's
                // necessary or not.
                if (expression.IsKind(SyntaxKind.InvocationExpression))
                {
                    expression = ((InvocationExpressionSyntax)expression).Expression;
                }

                string textOpt = null;
                if (expression is TypeSyntax typeSyntax && typeSyntax.IsVar)
                {
                    // If the user is hovering over 'var', then pass back the full type name that 'var'
                    // binds to.
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var type = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                    if (type != null)
                    {
                        textOpt = type.ToDisplayString() /*type.ToNameDisplayString()*/;
                    }
                }

                return new DebugDataTipInfo(expression.Span, textOpt);
            }
            catch (Exception e) //when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                Log.Warn(e.Message);
                return default(DebugDataTipInfo);
            }
        }

        internal static DebugDataTipInfo GetInfo(SyntaxNode root, SemanticModel semanticModel, SyntaxNode node, string textOpt, CancellationToken cancellationToken)
        {
            var expression = node as ExpressionSyntax;
            if (expression == null)
            {
                if (node is MethodDeclarationSyntax)
                {
                    return default(DebugDataTipInfo);
                }
                if (semanticModel != null)
                {
                    if (node is PropertyDeclarationSyntax)
                    {
                        var propertySymbol = semanticModel.GetDeclaredSymbol((PropertyDeclarationSyntax)node);
                        if (propertySymbol.IsStatic)
                        {
                            textOpt = propertySymbol.ContainingType.GetFullName() + "." + propertySymbol.Name;
                        }
                    }
                    else if (node.GetAncestor<FieldDeclarationSyntax>() != null)
                    {
                        var fieldSymbol = semanticModel.GetDeclaredSymbol(node.GetAncestorOrThis<VariableDeclaratorSyntax>());
                        if (fieldSymbol.IsStatic)
                        {
                            textOpt = fieldSymbol.ContainingType.GetFullName() + "." + fieldSymbol.Name;
                        }
                    }
                }

                return new DebugDataTipInfo(node.Span, text: textOpt);
            }

            if (expression.IsAnyLiteralExpression())
            {
                // If the user hovers over a literal, give them a DataTip for the type of the
                // literal they're hovering over.
                // Partial semantics should always be sufficient because the (unconverted) type
                // of a literal can always easily be determined.
                var type = semanticModel?.GetTypeInfo(expression, cancellationToken).Type;
                return type == null
                    ? default(DebugDataTipInfo)
                        : new DebugDataTipInfo(expression.Span, type.GetFullName());
            }

            // Check if we are invoking method and if we do return null so we don't invoke it
            if (expression.Parent is InvocationExpressionSyntax || semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol)
                return default(DebugDataTipInfo);

            if (expression.IsRightSideOfDotOrArrow())
            {
                var curr = expression;
                while (true)
                {
                    var conditionalAccess = curr.GetParentConditionalAccessExpression();
                    if (conditionalAccess == null)
                    {
                        break;
                    }

                    curr = conditionalAccess;
                }

                if (curr == expression)
                {
                    // NB: Parent.Span, not Span as below.
                    return new DebugDataTipInfo(expression.Parent.Span, text: null);
                }

                // NOTE: There may not be an ExpressionSyntax corresponding to the range we want.
                // For example, for input a?.$$B?.C, we want span [|a?.B|]?.C.
                return new DebugDataTipInfo(TextSpan.FromBounds(curr.SpanStart, expression.Span.End), text: null);
            }

            var typeSyntax = expression as TypeSyntax;
            if (typeSyntax != null && typeSyntax.IsVar)
            {
                // If the user is hovering over 'var', then pass back the full type name that 'var'
                // binds to.
                var type = semanticModel?.GetTypeInfo(typeSyntax, cancellationToken).Type;
                if (type != null)
                {
                    textOpt = type.GetFullName();
                }
            }

            if (semanticModel != null)
            {
                if (expression is IdentifierNameSyntax)
                {
                    if (expression.Parent is ObjectCreationExpressionSyntax)
                    {
                        var type = (INamedTypeSymbol)semanticModel.GetSymbolInfo(expression).Symbol;
                        if (type != null)
                            textOpt = type.GetFullName();
                    }
                    else if (expression.Parent is AssignmentExpressionSyntax && expression.Parent.Parent is InitializerExpressionSyntax)
                    {
                        var variable = expression.GetAncestor<VariableDeclaratorSyntax>();
                        if (variable != null)
                        {
                            textOpt = variable.Identifier.Text + "." + ((IdentifierNameSyntax)expression).Identifier.Text;
                        }
                    }

                }
            }
            return new DebugDataTipInfo(expression.Span, textOpt);
        }
    }

    public struct DebugDataTipInfo
    {
        public readonly TextSpan Span;
        public readonly string Text;

        public DebugDataTipInfo(TextSpan span, string text)
        {
            Span = span;
            Text = text;
        }

        public bool IsDefault
        {
            get { return Span.Length == 0 && Span.Start == 0 && Text == null; }
        }
    }
}
