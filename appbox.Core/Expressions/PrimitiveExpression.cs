using System;
using System.Linq.Expressions;
using System.Text;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class PrimitiveExpression : Expression
    {

        public object Value { get; set; }

        public override ExpressionType Type => ExpressionType.PrimitiveExpression;

        #region ====Ctor====

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal PrimitiveExpression() { }

        public PrimitiveExpression(object value) => Value = value;

        #endregion

        #region ====Overrides Methods====
        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            return System.Linq.Expressions.Expression.Constant(Value); //TODO: null & type
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            if (Value == null)
            {
                sb.Append("null");
                return;
            }
            if (Value is bool)
            {
                sb.Append((bool)Value == true ? "true" : "false");
                return;
            }
            if (Value is int || Value is decimal || Value is byte || Value is float)
            {
                sb.Append(Value);
                return;
            }
            if (Value is string || Value is char || Value is Guid || Value is DateTime)
            {
                sb.AppendFormat("\"{0}\"", Value);
                return;
            }
            if (Value.GetType().IsEnum)
            {
                sb.Append((int)Value);
                return;
            }

            throw new NotSupportedException();
        }
        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer bs)
        {
            bs.Serialize(Value);
        }

        public override void ReadObject(BinSerializer bs)
        {
            Value = bs.Deserialize();
        }
        #endregion

    }
}
