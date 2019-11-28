using System;
using System.Text;
using appbox.Expressions;
using appbox.Serialization;

namespace appbox.Store
{
    public sealed class SqlSelectItemExpression : Expression
    {
        public string AliasName { get; internal set; }

        public ISqlSelectQuery Owner { get; internal set; }

        public Expression Expression { get; internal set; }

        public override ExpressionType Type => ExpressionType.SelectItemExpression;

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal SqlSelectItemExpression() { }

        public SqlSelectItemExpression(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.FieldExpression:
                    //case ExpressionType.AggregationRefFieldExpression:
                    Expression = expression;
                    AliasName = ((MemberExpression)expression).Name;
                    break;
                case ExpressionType.SelectItemExpression:
                    Expression = expression;
                    AliasName = ((SqlSelectItemExpression)expression).AliasName;
                    break;
                default:
                    Expression = expression;
                    AliasName = "unnamed";
                    break;
            }
        }

        public SqlSelectItemExpression(Expression expression, string aliasName)
        {
            Expression = expression;
            AliasName = aliasName;
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            Expression.ToCode(sb, preTabs);
            sb.Append(" As ");
            sb.Append(AliasName);
        }

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer bs)
        {
            bs.Serialize(Expression);
            bs.Write(AliasName);
            bs.Serialize(Owner);
        }

        public override void ReadObject(BinSerializer bs)
        {
            Expression = (Expression)bs.Deserialize();
            AliasName = bs.ReadString();
            Owner = (ISqlSelectQuery)bs.Deserialize();
        }
        #endregion
    }
}
