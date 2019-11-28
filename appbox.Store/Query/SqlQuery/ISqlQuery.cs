using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    public interface ISqlQuery
    {
        Expression Filter { get; }
    }

    public interface ISqlSelectQuery : ISqlQuery
    {
        Dictionary<string, SqlSelectItemExpression> Selects { get; }

        List<SqlSortItem> SortItems { get; }

        bool HasSortItems { get; }

        /// <summary>
        /// 是否外部查询
        /// </summary>
        bool IsOutQuery { get; }

        ///// <summary>
        ///// 数据库的名称
        ///// </summary>
        //string StoreName { get; }

        #region ====分页查询属性====

        /// <summary>
        /// Top或分页大小
        /// </summary>
        int TopOrPageSize { get; }

        /// <summary>
        /// 分页索引号
        /// </summary>
        /// <remarks>
        /// 注意：-1为不分页
        /// </remarks>
        int PageIndex { get; }

        #endregion

        /// <summary>
        /// 查询的用途
        /// </summary>
        QueryPurpose Purpose { get; }

        bool Distinct { get; }

    }

    public enum QueryPurpose : byte
    {
        None,
        ToScalar,
        ToDataTable,
        ToEntityTreeList,
        ToSingleEntity,
        ToEntityList,
        ToTreeNodePath
    }
}
