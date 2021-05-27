using System;
using System.Text;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class IfStatementExpression : Expression
    {
        public Expression Condition { get; private set; }

        public BlockExpression TrueStatement { get; private set; }

        public Expression FalseStatement { get; private set; } //注意：类型非BlockExpression，可能还是IfStatementExpression

        public override ExpressionType Type
        {
            get { return ExpressionType.IfStatementExpression; }
        }

		public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            return System.Linq.Expressions.Expression.IfThenElse(Condition.ToLinqExpression(ctx), TrueStatement.ToLinqExpression(ctx), FalseStatement.ToLinqExpression(ctx)); //TODO: null & type
        }

        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal IfStatementExpression() { }

        /// <summary>
        /// 仅用于设计时
        /// </summary>
        public IfStatementExpression(Expression condition, BlockExpression trueStatement, Expression falseStatement)
        {
            if (object.Equals(null, condition))
                throw new ArgumentNullException(nameof(condition));

            this.Condition = condition;
            this.TrueStatement = trueStatement;
            this.FalseStatement = falseStatement;
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            sb.Append("if (");
            this.Condition.ToCode(sb, preTabs);
            sb.AppendLine(")");
            sb.Append(preTabs);
            sb.AppendLine("{");
            preTabs = preTabs + "\t";
            this.TrueStatement.ToCode(sb, preTabs);
            preTabs = preTabs.Remove(0, 1);
            sb.AppendLine();
            sb.Append(preTabs);
            sb.Append("}");

            if (!object.Equals(null, FalseStatement))
            {
                sb.AppendLine();
                sb.Append(preTabs);
                sb.Append("else ");
                if (FalseStatement is IfStatementExpression)
                {
                    FalseStatement.ToCode(sb, preTabs);
                }
                else
                {
                    sb.AppendLine();
                    sb.Append(preTabs);
                    sb.AppendLine("{");

                    preTabs = preTabs + "\t";
                    FalseStatement.ToCode(sb, preTabs);
                    preTabs = preTabs.Remove(0, 1);

                    sb.AppendLine();
                    sb.Append(preTabs);
                    sb.Append("}");
                }
            }
        }

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Serialize(this.Condition);
            bs.Serialize(this.TrueStatement);
            bs.Serialize(this.FalseStatement);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            this.Condition = (Expression)bs.Deserialize();
            this.TrueStatement = (BlockExpression)bs.Deserialize();
            this.FalseStatement = (Expression)bs.Deserialize();
        }
        #endregion
    }
}

