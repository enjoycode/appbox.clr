using System;
using System.Text;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class BinaryExpression : Expression
    {

        #region ====Properties====

        public Expression LeftOperand { get; private set; }

        public BinaryOperatorType BinaryType { get; private set; }

        public Expression RightOperand { get; private set; }

        public override ExpressionType Type => ExpressionType.BinaryExpression;

        #endregion

        #region ====Ctor====

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal BinaryExpression() { }

        public BinaryExpression(Expression leftOperator, Expression rightOperator, BinaryOperatorType operatorType)
        {
            LeftOperand = Equals(null, leftOperator) ? new PrimitiveExpression(null) : leftOperator;
            RightOperand = Equals(null, rightOperator) ? new PrimitiveExpression(null) : rightOperator;
            BinaryType = operatorType;
        }

        #endregion

        #region ====Overrides Methods====
        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            //TODO: 暂简单处理Nullable问题
            //TODO: 优化TupleField<String>的相关比较，直接用原生utf8，不要再转换为C#字符串
            var left = LeftOperand.ToLinqExpression(ctx);
            var right = RightOperand.ToLinqExpression(ctx);
            if (LeftOperand.Type == ExpressionType.KVFieldExpression)
            {
                var tupleExp = (KVFieldExpression)LeftOperand;
                if (!(tupleExp.IsClassType()
                    || RightOperand.Type == ExpressionType.KVFieldExpression
                    || (RightOperand.Type == ExpressionType.PrimitiveExpression && ((PrimitiveExpression)RightOperand).Value == null)))
                {
                    right = System.Linq.Expressions.Expression.Convert(right, left.Type);
                }
            }
            else if (RightOperand.Type == ExpressionType.KVFieldExpression)
            {
                var tupleExp = (KVFieldExpression)RightOperand;
                if (!(tupleExp.IsClassType()
                    || LeftOperand.Type == ExpressionType.KVFieldExpression
                     || (LeftOperand.Type == ExpressionType.PrimitiveExpression && ((PrimitiveExpression)LeftOperand).Value == null)))
                {
                    left = System.Linq.Expressions.Expression.Convert(left, right.Type);
                }
            }

            System.Linq.Expressions.ExpressionType type;
            switch (BinaryType)
            {
                case BinaryOperatorType.Equal:
                    type = System.Linq.Expressions.ExpressionType.Equal; break;
                case BinaryOperatorType.NotEqual:
                    type = System.Linq.Expressions.ExpressionType.NotEqual; break;
                case BinaryOperatorType.Greater:
                    type = System.Linq.Expressions.ExpressionType.GreaterThan; break;
                case BinaryOperatorType.GreaterOrEqual:
                    type = System.Linq.Expressions.ExpressionType.GreaterThanOrEqual; break;
                case BinaryOperatorType.Less:
                    type = System.Linq.Expressions.ExpressionType.LessThan; break;
                case BinaryOperatorType.LessOrEqual:
                    type = System.Linq.Expressions.ExpressionType.LessThanOrEqual; break;
                default:
                    throw ExceptionHelper.NotImplemented();
            }


            return System.Linq.Expressions.Expression.MakeBinary(type, left, right);
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            //Todo:判断In,Like等特殊语法进行方法转换，否则解析器无法解析
            if (BinaryType == BinaryOperatorType.Like)
            {
                sb.Append("f.Contains(");
                LeftOperand.ToCode(sb, preTabs);
                sb.Append(",");
                RightOperand.ToCode(sb, preTabs);
                sb.Append(")");
            }
            else
            {
                LeftOperand.ToCode(sb, preTabs);
                sb.AppendFormat(" {0} ", GetBinaryOperatorTypeString());
                RightOperand.ToCode(sb, preTabs);
            }
        }

        private string GetBinaryOperatorTypeString()
        {
            string bt;
            switch (BinaryType)
            {
                case BinaryOperatorType.BitwiseAnd:
                    bt = "&";
                    break;
                case BinaryOperatorType.BitwiseOr:
                    bt = "|";
                    break;
                case BinaryOperatorType.BitwiseXor:
                    bt = "Xor";
                    break;
                case BinaryOperatorType.Divide:
                    bt = "/";
                    break;
                case BinaryOperatorType.Equal:
                    bt = "==";
                    break;
                case BinaryOperatorType.Greater:
                    bt = ">";
                    break;
                case BinaryOperatorType.GreaterOrEqual:
                    bt = ">=";
                    break;
                case BinaryOperatorType.In:
                    bt = "In";
                    break;
                case BinaryOperatorType.Is:
                    bt = "Is";
                    break;
                case BinaryOperatorType.IsNot:
                    bt = "IsNot";
                    break;
                case BinaryOperatorType.Less:
                    bt = "<";
                    break;
                case BinaryOperatorType.LessOrEqual:
                    bt = "<=";
                    break;
                case BinaryOperatorType.Like:
                    bt = "Like";
                    break;
                case BinaryOperatorType.Minus:
                    bt = "-";
                    break;
                case BinaryOperatorType.Modulo:
                    bt = "Mod";
                    break;
                case BinaryOperatorType.Multiply:
                    bt = "*";
                    break;
                case BinaryOperatorType.NotEqual:
                    bt = "!=";
                    break;
                case BinaryOperatorType.Plus:
                    bt = "+";
                    break;
                case BinaryOperatorType.As:
                    bt = "as";
                    break;
                default:
                    throw new NotSupportedException();
            }
            return bt;
        }

        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer bs)
        {
            bs.Serialize(LeftOperand);
            bs.Write((byte)BinaryType);
            bs.Serialize(RightOperand);
        }

        public override void ReadObject(BinSerializer bs)
        {
            LeftOperand = (Expression)bs.Deserialize();
            BinaryType = (BinaryOperatorType)bs.ReadByte();
            RightOperand = (Expression)bs.Deserialize();
        }
        #endregion

    }

    public enum BinaryOperatorType
    {
        BitwiseAnd = 7,
        BitwiseOr = 8,
        BitwiseXor = 9,
        Divide = 10,
        Equal = 0,
        Greater = 2,
        GreaterOrEqual = 5,
        In = 17,
        Is = 16,
        IsNot = 15,
        Less = 3,
        LessOrEqual = 4,
        Like = 6,
        Minus = 14,
        Modulo = 11,
        Multiply = 12,
        NotEqual = 1,
        Plus = 13,
        As = 18,
        AndAlso = 19,
        OrElse = 20,
        Assign = 21,
    }
}
