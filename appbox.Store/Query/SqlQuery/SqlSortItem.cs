using System;
using appbox.Expressions;

namespace appbox.Store
{
    public struct SqlSortItem
    {
        public Expression Expression { get; }

        public SortType SortType { get; }

        public SqlSortItem(Expression sortItem)
            : this(sortItem, SortType.ASC)
        {
        }

        public SqlSortItem(Expression sortItem, SortType sortType)
        {
            if (Equals(null, sortItem))
                throw new ArgumentNullException(nameof(sortItem));

            if (sortItem.Type != ExpressionType.FieldExpression
                && sortItem.Type != ExpressionType.SelectItemExpression)
                throw new ArgumentException("sortItem is not FieldExpression or QuerySelectExpression");

            Expression = sortItem;
            SortType = sortType;
        }
    }

    public enum SortType : byte //TODO: rename to SortBy
    {
        ASC,
        DESC
    }
}
