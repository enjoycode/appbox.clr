using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    public interface ISqlQuery
    {
        /// <summary>
        /// 过滤条件
        /// </summary>
        Expression Filter { get; }
    }

    public interface ISqlSelectQuery : ISqlQuery
    {
        Dictionary<string, SqlSelectItemExpression> Selects { get; }

        List<SqlSortItem> SortItems { get; }

        bool HasSortItems { get; }

        int TakeSize { get; }

        int SkipSize { get; }

        /// <summary>
        /// 查询的用途
        /// </summary>
        QueryPurpose Purpose { get; }

        bool Distinct { get; }

        /// <summary>
        /// 分组字段
        /// </summary>
        SqlSelectItemExpression[] GroupByKeys { get; }

        /// <summary>
        /// 分组过滤条件
        /// </summary>
        Expression HavingFilter { get; }
    }

    public enum QueryPurpose : byte
    {
        None,
        Count,
        ToScalar,
        ToDataTable,
        ToEntityTreeList,
        ToSingleEntity,
        ToEntityList,
        ToTreeNodePath
    }
}
