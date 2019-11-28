using System;
using System.Linq;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.ServiceInterceptors
{
    /// <summary>
    /// 将IndexScan.Kes.XXX(t => t.YYY, value)转换为IndexScan.Keys.Where(KeyPredicate)
    /// </summary>
    sealed class IndexPredicateInterceptor : IInvocationInterceptor<SyntaxNode>
    {
        internal const string Name = "IndexPredicate";

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
                throw new ArgumentException("KeyPredicate参数错误");

            var valueExp = node.ArgumentList.Arguments[1].Expression;
            valueExp = (ExpressionSyntax)valueExp.Accept(visitor);

            ServiceCodeGenerator generator = (ServiceCodeGenerator)visitor;
            var entitySymbol = generator.SemanticModel.GetSymbolInfo(memberAccess).Symbol; //sys.Entities.Emploee.IndexName.FieldName
            var names = entitySymbol.ToString().Split('.');
            var appNode = generator.hub.DesignTree.FindApplicationNodeByName(names[0]);
            var entityModelNode = generator.hub.DesignTree.FindModelNodeByName(appNode.Model.Id, ModelType.Entity, names[2]);
            var entityModel = (EntityModel)entityModelNode.Model;
            ushort memberId = entityModel.GetMember(memberAccess.Name.Identifier.Text, true).MemberId;
            var indexModel = entityModel.SysStoreOptions.Indexes.Single(t => t.Name == names[3]);
            int fieldIndex = Array.FindIndex(indexModel.Fields, t => t.MemberId == memberId);

            var arg1Exp = SyntaxFactory.ParseExpression($"new appbox.Store.KeyPredicate({memberId}, appbox.Store.KeyPredicateType.{oldMemExp.Name}, {valueExp})");
            var arg1 = SyntaxFactory.Argument(arg1Exp);
            var arg2Exp = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(fieldIndex));
            var arg2 = SyntaxFactory.Argument(arg2Exp);
            var arg3Exp = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(indexModel.Fields.Length)); //索引字段个数
            var arg3 = SyntaxFactory.Argument(arg3Exp);
            var argList = SyntaxFactory.ArgumentList().AddArguments(arg1, arg2, arg3);

            var res = SyntaxFactory.InvocationExpression(newMemExp, argList);
            return res.WithTriviaFrom(node);
        }
    }
}
