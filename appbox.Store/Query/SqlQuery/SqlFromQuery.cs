using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    public sealed class SqlFromQuery : SqlQueryBase, ISqlSelectQuery
    {

        #region ====Fields & Properties====

        private Dictionary<string, SqlSelectItemExpression> _t;
        private Dictionary<string, SqlSelectItemExpression> _selects;
        private List<SqlSortItem> _sortItems;

        public ISqlSelectQuery Target { get; }

        public Dictionary<string, SqlSelectItemExpression> T
        {
            get
            {
                if (_t == null)
                {
                    _t = new Dictionary<string, SqlSelectItemExpression>();
                    foreach (var item in Target.Selects.Values)
                    {
                        SqlSelectItemExpression si = new SqlSelectItemExpression(item);
                        si.Owner = this;
                        _t.Add(item.AliasName, si);
                    }
                }
                return _t;
            }
        }

        public Dictionary<string, SqlSelectItemExpression> Selects
        {
            get
            {
                if (_selects == null)
                    _selects = new Dictionary<string, SqlSelectItemExpression>();
                return _selects;
            }
        }

        public List<SqlSortItem> SortItems
        {
            get
            {
                if (_sortItems == null)
                    _sortItems = new List<SqlSortItem>();
                return _sortItems;
            }
        }

        public bool HasSortItems
        {
            get { return _sortItems != null && _sortItems.Count > 0; }
        }

        /// <summary>
        /// 筛选器
        /// </summary>
        public Expression Filter { get; set; }

        public bool IsOutQuery
        {
            get { return true; }
        }

        public int TopOrPageSize { get; private set; }

        public int PageIndex { get; private set; }

        public QueryPurpose Purpose { get; }

        public bool Distinct { get; set; }

        #endregion

        #region ====Ctor====

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal SqlFromQuery() { }

        internal SqlFromQuery(ISqlSelectQuery target)
        {
            Target = target;
        }

        #endregion

        #region ====Select Methods====

        //public AppBox.Core.DataTable ToDataTable(params SelectItem[] selectItem)
        //{
        //    return this.ToDataTable(0, -1, selectItem);
        //}

        //public AppBox.Core.DataTable ToDataTable(int topSize, params SelectItem[] selectItem)
        //{
        //    return this.ToDataTable(topSize, -1, selectItem);
        //}

        //public AppBox.Core.DataTable ToDataTable(int pageSize, int pageIndex, params SelectItem[] selectItem)
        //{
        //    if (selectItem == null || selectItem.Length <= 0)
        //        throw new System.ArgumentException("must select some one");

        //    this._topOrPageSize = pageSize;
        //    this._pageIndex = pageIndex;

        //    this._purpose = QueryPurpose.ToDataTable;

        //    foreach (var item in selectItem)
        //    {
        //        if (item.Target.Expression.Type == ExpressionType.SelectItemExpression)
        //        {
        //            SelectItemExpression si = (SelectItemExpression)item.Target.Expression;
        //            if (si.Owner == this)
        //            {
        //                SelectItemExpression tar = new SelectItemExpression(si.Expression, item.Target.AliasName);
        //                tar.Owner = this;
        //                this.Selects.Add(item.Target.AliasName, tar);
        //            }
        //            else
        //                this.Selects.Add(item.Target.AliasName, item.Target);
        //        }
        //        else
        //            this.Selects.Add(item.Target.AliasName, item.Target);
        //    }

        //    //递交查询
        //    var db = SqlStore.Get(this.StoreName);
        //    var cmd = db.DbCommandBuilder.CreateQueryCommand(this);
        //    return db.ExecuteToTable(cmd);
        //}

        /// <summary>
        /// 查找FromQuery的目标Query的EntityExpression，用于获取IRuntimeContext执行查询或获取StoreName
        /// </summary>
        /// <returns>The sys context.</returns>
        /// <param name="oq">Oq.</param>
        private static EntityExpression FindTargetEntityExpression(SqlFromQuery oq)
        {
            var query = oq.Target as SqlQuery;
            return query != null ? query.T : FindTargetEntityExpression((SqlFromQuery)oq.Target);
        }

        public SqlSubQuery ToSubQuery(params SqlSelectItem[] selectItem)
        {
            return this.ToSubQuery(0, selectItem);
        }

        public SqlSubQuery ToSubQuery(int topSize, params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            foreach (var item in selectItem)
            {
                if (item.Target.Expression.Type == ExpressionType.SelectItemExpression)
                {
                    SqlSelectItemExpression si = (SqlSelectItemExpression)item.Target.Expression;
                    if (si.Owner == this)
                    {
                        SqlSelectItemExpression tar = new SqlSelectItemExpression(si.Expression, item.Target.AliasName);
                        tar.Owner = this;
                        Selects.Add(item.Target.AliasName, tar);
                    }
                    else
                        Selects.Add(item.Target.AliasName, item.Target);
                }
                else
                    Selects.Add(item.Target.AliasName, item.Target);
            }

            TopOrPageSize = topSize;
            PageIndex = -1;

            return new SqlSubQuery(this);
        }

        public SqlFromQuery ToOutQuery(params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            foreach (var item in selectItem)
            {
                if (item.Target.Expression.Type == ExpressionType.SelectItemExpression)
                {
                    SqlSelectItemExpression si = (SqlSelectItemExpression)item.Target.Expression;
                    if (si.Owner == this)
                    {
                        SqlSelectItemExpression tar = new SqlSelectItemExpression(si.Expression, item.Target.AliasName);
                        tar.Owner = this;
                        Selects.Add(item.Target.AliasName, tar);
                    }
                    else
                        Selects.Add(item.Target.AliasName, item.Target);
                }
                else
                    Selects.Add(item.Target.AliasName, item.Target);
            }

            return new SqlFromQuery(this);
        }

        #endregion

        #region ====Where Methods====
        public SqlFromQuery Where(Expression condition)
        {
            Filter = condition;
            return this;
        }

        public SqlFromQuery AndWhere(Expression condition)
        {
            Filter = new BinaryExpression(Filter, condition, BinaryOperatorType.AndAlso);
            return this;
        }

        public SqlFromQuery OrWhere(Expression conditin)
        {
            Filter = new BinaryExpression(Filter, conditin, BinaryOperatorType.OrElse);
            return this;
        }
        #endregion

        #region ====OrderBy Methods====
        public SqlFromQuery OrderBy(Expression sortItem)
        {
            return OrderBy(sortItem, SortType.ASC);
        }

        public SqlFromQuery OrderBy(Expression sortItem, SortType sortType)
        {
            SqlSortItem sort = new SqlSortItem(sortItem, sortType);
            SortItems.Add(sort);
            return this;
        }
        #endregion

    }
}
