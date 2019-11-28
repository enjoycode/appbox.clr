using System;
using appbox.Expressions;

namespace appbox.Store
{
    public sealed class SqlJoin
    {
        public ISqlQueryJoin Right { get; }

        public Expression OnConditon { get; }

        public JoinType JoinType { get; }

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal SqlJoin() { }

        public SqlJoin(ISqlQueryJoin right, JoinType joinType, Expression onCondition)
        {
            Right = right;
            JoinType = joinType;
            OnConditon = onCondition;
        }

    }

    /// <summary>
    /// 查询联接类型
    /// </summary>
    /// <remarks>对应数据库查询的联接类型</remarks>
    public enum JoinType : byte
    {
        InnerJoin,
        LeftJoin,
        RightJoin,
        FullJoin
    }
}
