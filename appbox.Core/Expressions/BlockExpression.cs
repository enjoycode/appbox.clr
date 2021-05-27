using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class BlockExpression : Expression
    {

        private List<Expression> statements = new List<Expression>();
        public List<Expression> Statements
        {
            get { return statements; }
        }

        public override ExpressionType Type => ExpressionType.BlockExpression;

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                //if (i != 0)
                sb.Append(preTabs);
                statements[i].ToCode(sb, preTabs);

                if (!(statements[i] is IfStatementExpression)) //todo:暂简单排除Statement
                {
                    sb.Append(";");
                }

                if (i != statements.Count - 1)
                    sb.AppendLine();
            }
        }
		public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
		{
			var ss = from s in statements
					 select s.ToLinqExpression(ctx);
			return System.Linq.Expressions.Expression.Block(ss);
		}

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.WriteList<Expression>(statements);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            this.statements = bs.ReadList<Expression>();
        }
        #endregion

    }
}

