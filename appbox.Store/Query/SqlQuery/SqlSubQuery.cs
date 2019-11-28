using System;
using System.Collections.Generic;
using System.Text;
using appbox.Expressions;

namespace appbox.Store
{
    /// <summary>
    /// 子查询
    /// 用于将外部查询[OutQuery]或查询[Query]包装为子查询
    /// </summary>
    public sealed class SqlSubQuery : Expression, ISqlQueryJoin
    {

        #region ====Fields====
        private List<SqlJoin> _joins;

        public ISqlSelectQuery Target { get; }

        public Dictionary<string, SqlSelectItemExpression> T
        {
            get { return Target.Selects; }
        }

        public bool HasJoins
        {
            get { return _joins != null && _joins.Count > 0; }
        }

        public List<SqlJoin> Joins
        {
            get
            {
                if (_joins == null)
                    _joins = new List<SqlJoin>();
                return _joins;
            }
        }

        public override ExpressionType Type => ExpressionType.SubQueryExpression;
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal SqlSubQuery() { }

        internal SqlSubQuery(ISqlSelectQuery target)
        {
            Target = target;
        }
        #endregion

        #region ====Overrides====
        public override void ToCode(StringBuilder sb, string preTabs)
        {
            sb.AppendFormat("SubQuery({0})", Target); //TODO: fix target.ToCode
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ====Join Methods====
        public ISqlQueryJoin InnerJoin(ISqlQueryJoin target, Expression onCondition)
        {
            return Join(JoinType.InnerJoin, target, onCondition);
        }

        public ISqlQueryJoin LeftJoin(ISqlQueryJoin target, Expression onCondition)
        {
            return Join(JoinType.LeftJoin, target, onCondition);
        }

        public ISqlQueryJoin RightJoin(ISqlQueryJoin target, Expression onCondition)
        {
            return Join(JoinType.RightJoin, target, onCondition);
        }

        public ISqlQueryJoin FullJoin(ISqlQueryJoin target, Expression onCondition)
        {
            return Join(JoinType.FullJoin, target, onCondition);
        }

        private ISqlQueryJoin Join(JoinType join, ISqlQueryJoin target, Expression onCondition)
        {
            if (Equals(null, target) || Equals(null, onCondition))
                throw new ArgumentNullException();

            Joins.Add(new SqlJoin(target, join, onCondition));
            return target;
        }
        #endregion

    }
}
