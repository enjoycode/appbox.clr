using System;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将sys.Permissions.Admin 转换为 appbox.Runtime.RuntimeContext.HasPermission(id)
    /// </summary>
    sealed class PermissionAccessInterceptor : IMemberAccessInterceptor<SyntaxNode>
    {

        internal const string Name = "PermissionAccess";

        public SyntaxNode VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var appName = symbol.ContainingType.ContainingNamespace.Name;
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(appName);
            var modelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Permission, symbol.Name);

            return SyntaxFactory.ParseExpression($"appbox.Runtime.RuntimeContext.HasPermission({modelNode.Model.Id})");
        }
    }
}
