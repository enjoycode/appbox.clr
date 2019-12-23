using System;
using System.Text;
using appbox.Expressions;

namespace appbox.Store
{
    public sealed class DbFuncExpression : Expression //InvocationExpression
    {

        public override ExpressionType Type => ExpressionType.DbFuncExpression;
        public DbFuncName Name { get; private set; }
        public Expression[] Parameters { get; private set; }

        internal DbFuncExpression(DbFuncName name, params Expression[] paras)
        {
            Name = name;
            Parameters = paras;
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            sb.Append($"{Name}()");
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }
    }

    public enum DbFuncName
    {
        Sum,
        Avg,
        Max,
        Min,
        Cast
    }
}
