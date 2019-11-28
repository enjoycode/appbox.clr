using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class GroupExpression : Expression //TODO: remove it
    {

        #region ====Fields & Properties====
        List<Expression> _operands = new List<Expression>();

        public Expression[] Operands
        {
            get { return _operands.ToArray(); }
        }

        public GroupOperatorType GroupType { get; private set; }

        public override ExpressionType Type => ExpressionType.GroupExpression;
        #endregion

        #region ====Ctor====

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal GroupExpression() { }

        public GroupExpression(GroupOperatorType type, Expression[] operands)
        {
            GroupType = type;
            _operands.AddRange(operands);
        }

        #endregion

        #region ====static Combine Method====

        public static Expression Combine(GroupOperatorType opType, Expression left, Expression right)
        {
            Expression[] collection1;
            Expression[] collection2;
            if (ReferenceEquals(left, null))
                return right;
            if (ReferenceEquals(right, null))
                return left;

            GroupExpression operator1 = left as GroupExpression;
            if (((!ReferenceEquals(operator1, null) && (operator1.GroupType != opType)) ? 1 : 0) != 0)
                operator1 = null;
            GroupExpression operator2 = right as GroupExpression;
            if (((!ReferenceEquals(operator2, null) && (operator2.GroupType != opType)) ? 1 : 0) != 0)
                operator2 = null;
            if (ReferenceEquals(operator1, null))
                collection1 = new Expression[] { left };
            else
                collection1 = operator1._operands.ToArray();
            if (ReferenceEquals(operator2, null))
                collection2 = new Expression[] { right };
            else
                collection2 = operator2._operands.ToArray();
            GroupExpression operator3 = new GroupExpression(opType, new Expression[0]);
            operator3._operands.AddRange(collection1);
            operator3._operands.AddRange(collection2);
            return operator3;
        }

        #endregion

        #region ====Overrides Methods====
        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            System.Linq.Expressions.ExpressionType type = System.Linq.Expressions.ExpressionType.Equal;
            switch (GroupType)
            {
                case GroupOperatorType.And:
                    type = System.Linq.Expressions.ExpressionType.AndAlso; break;
                case GroupOperatorType.Or:
                    type = System.Linq.Expressions.ExpressionType.OrElse; break;
                default:
                    throw ExceptionHelper.NotImplemented();
            }

            System.Linq.Expressions.Expression left = _operands[0].ToLinqExpression(ctx);
            for (int i = 1; i < _operands.Count; i++)
            {
                var right = _operands[i].ToLinqExpression(ctx);
                left = System.Linq.Expressions.Expression.MakeBinary(type, left, right);
            }
            return left;
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            string bt = GetGroupTypeString();

            sb.Append("(");
            for (int i = 0; i < Operands.Length; i++)
            {
                Operands[i].ToCode(sb, preTabs);
                if (i != Operands.Length - 1)
                {
                    sb.Append(" ");
                    sb.Append(bt);
                    sb.Append(" ");
                }
            }
            sb.Append(")");
        }
        #endregion

        #region ====Private Help Methods====
        private string GetGroupTypeString()
        {
            string bt;
            switch (GroupType)
            {
                case GroupOperatorType.And:
                    bt = "And";
                    break;
                case GroupOperatorType.Or:
                    bt = "Or";
                    break;
                case GroupOperatorType.Add:
                    bt = "+";
                    break;
                case GroupOperatorType.Subtract:
                    bt = "-";
                    break;
                case GroupOperatorType.Multiply:
                    bt = "*";
                    break;
                case GroupOperatorType.Divide:
                    bt = "/";
                    break;
                case GroupOperatorType.Mod:
                    bt = "Mod";
                    break;
                default:
                    bt = "UnKnown";
                    break;
            }
            return bt;
        }
        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer writer)
        {
            writer.Write((byte)GroupType);
            writer.WriteList(_operands);
        }

        public override void ReadObject(BinSerializer reader)
        {
            GroupType = (GroupOperatorType)reader.ReadByte();
            _operands = reader.ReadList<Expression>();
        }
        #endregion

    }

    public enum GroupOperatorType : byte
    {
        And,
        Or,
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod
    }
}
