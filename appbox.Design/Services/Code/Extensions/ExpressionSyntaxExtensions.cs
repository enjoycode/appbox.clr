using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design
{
    static class ExpressionSyntaxExtensions
    {
        public static bool IsAnyLiteralExpression(this ExpressionSyntax expression)
        {
            return
                expression.IsKind(SyntaxKind.CharacterLiteralExpression) ||
                expression.IsKind(SyntaxKind.FalseLiteralExpression) ||
                expression.IsKind(SyntaxKind.NullLiteralExpression) ||
                expression.IsKind(SyntaxKind.NumericLiteralExpression) ||
                expression.IsKind(SyntaxKind.StringLiteralExpression) ||
                expression.IsKind(SyntaxKind.TrueLiteralExpression);
        }

        public static bool IsAnyMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            return expression != null && expression.Parent is MemberAccessExpressionSyntax && ((MemberAccessExpressionSyntax)expression.Parent).Name == expression;
        }

        public static bool IsRightSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Right == expression;
        }

        public static bool IsRightSideOfDotOrArrow(this ExpressionSyntax name)
        {
            return IsAnyMemberAccessExpressionName(name) || IsRightSideOfQualifiedName(name);
        }


    }
}
