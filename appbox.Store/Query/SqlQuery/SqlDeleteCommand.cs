using System;
using appbox.Expressions;

namespace appbox.Store
{
    /// <summary>
    /// 用于删除满足指定条件的记录
    /// </summary>
    public sealed class SqlDeleteCommand : SqlQueryBase, ISqlQuery
    {
        public EntityExpression T { get; }

        /// <summary>
        /// 筛选器
        /// </summary>
        public Expression Filter { get; set; }

        public SqlDeleteCommand(ulong entityModelID)
        {
            T = new EntityExpression(entityModelID, this);
        }

        public SqlDeleteCommand Where(Expression condition)
        {
            Filter = condition;
            return this;
        }
    }
}
