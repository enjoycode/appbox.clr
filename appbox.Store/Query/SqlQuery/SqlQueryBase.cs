using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    public abstract class SqlQueryBase
    {
        public string AliasName { get; set; }

        private List<SqlJoin> _joins;
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
