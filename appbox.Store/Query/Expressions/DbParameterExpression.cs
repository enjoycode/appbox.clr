using System;
using System.Text;
using System.Data.Common;
using appbox.Expressions;

namespace appbox.Store
{
    /// <summary>
    /// 作为参数占位用的数据库参数表达式
    /// </summary>
    public sealed class DbParameterExpression : Expression
    {
        public override ExpressionType Type => ExpressionType.DbParameterExpression;

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            throw new NotImplementedException();
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
