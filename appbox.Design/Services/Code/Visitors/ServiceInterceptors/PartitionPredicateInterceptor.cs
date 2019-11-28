using System;
using System.Linq;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将TableScan.Partitions.XXX(t => t.YYY, value)转换为TableScan.Partitions.Where(KeyPredicate)
    /// </summary>
    sealed class PartitionPredicateInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "PartitionPredicate";

        public SyntaxNode VisitInvocation(InvocationExpressionSyntax node, IMethodSymbol symbol, CSharpSyntaxVisitor<SyntaxNode> visitor)
        {
            //转换方法名称
            var oldMemExp = (MemberAccessExpressionSyntax)node.Expression;
            var newName = (SimpleNameSyntax)SyntaxFactory.ParseName("Where");
            var newMemExp = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, oldMemExp.Expression, newName);

            //处理参数转换为KeyPredicate
            var target = node.ArgumentList.Arguments[0].Expression;
            MemberAccessExpressionSyntax memberAccess = null;
            if (target is SimpleLambdaExpressionSyntax)
                memberAccess = ((SimpleLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;
            else if (target is ParenthesizedLambdaExpressionSyntax)
                memberAccess = ((ParenthesizedLambdaExpressionSyntax)target).Body as MemberAccessExpressionSyntax;
            if (memberAccess == null)
                throw new ArgumentException("PartitionPredicate参数错误");

            var valueExp = node.ArgumentList.Arguments[1].Expression;
            valueExp = (ExpressionSyntax)valueExp.Accept(visitor);

            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var entitySymbol = generator.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            var names = entitySymbol.ToString().Split('.');
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(names[0]);
            var entityModelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
            var entityModel = (EntityModel)entityModelNode.Model;
            ushort memberId = 0;
            if (memberAccess.Name.Identifier.Text != nameof(Data.Entity.CreateTime)) //注意排除特殊成员
            {
                memberId = entityModel.GetMember(memberAccess.Name.Identifier.Text, true).MemberId;
            }

            //判断成员是否分区键
            if (entityModel.SysStoreOptions.PartitionKeys == null)
                throw new Exception("非分区表不能指定分区谓词");
            int pkIndex = Array.FindIndex(entityModel.SysStoreOptions.PartitionKeys, t => t.MemberId == memberId);
            if (pkIndex == -1)
                throw new Exception("指定成员非分区键");

            var arg1Exp = SyntaxFactory.ParseExpression($"new appbox.Store.KeyPredicate({memberId}, appbox.Store.KeyPredicateType.{oldMemExp.Name}, {valueExp})");
            var arg1 = SyntaxFactory.Argument(arg1Exp);
            var arg2Exp = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(pkIndex));
            var arg2 = SyntaxFactory.Argument(arg2Exp);
            var arg3Exp = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(entityModel.SysStoreOptions.PartitionKeys.Length));
            var arg3 = SyntaxFactory.Argument(arg3Exp);

            var argList = SyntaxFactory.ArgumentList().AddArguments(arg1, arg2, arg3);

            var res = SyntaxFactory.InvocationExpression(newMemExp, argList);
            return res.WithTriviaFrom(node);
        }
    }
}
