using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design
{
    interface IMemberAccessInterceptor<T>
    {
        T VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol symbol, CSharpSyntaxVisitor<T> visitor);
    }

    interface IInvocationInterceptor<T>
    {
        T VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<T> visitor);
    }
}
