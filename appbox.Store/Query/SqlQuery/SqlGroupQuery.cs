using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Expressions;

namespace appbox.Store
{
    public sealed class SqlGroupQuery : ISqlSelectQuery
    {

        #region ====Fields & Properties====
        public ISqlSelectQuery GroupFrom { get; private set; }

        private Dictionary<string, SqlSelectItemExpression> _t;
        public Dictionary<string, SqlSelectItemExpression> T
        {
            get
            {
                if (_t == null)
                {
                    _t = new Dictionary<string, SqlSelectItemExpression>();
                    foreach (var item in GroupFrom.Selects.Values)
                    {
                        SqlSelectItemExpression si = new SqlSelectItemExpression(item);
                        si.Owner = this;
                        _t.Add(item.AliasName, si);
                    }
                }
                return _t;
            }
        }


        private Dictionary<string, SqlSelectItemExpression> _selects;
        public Dictionary<string, SqlSelectItemExpression> Selects
        {
            get
            {
                if (_selects == null)
                    _selects = new Dictionary<string, SqlSelectItemExpression>();
                return _selects;
            }
        }

        private List<SqlSortItem> _sortItems;

        public List<SqlSortItem> SortItems
        {
            get
            {
                if (_sortItems == null)
                    _sortItems = new List<SqlSortItem>();
                return _sortItems;
            }
        }

        public bool HasSortItems => _sortItems != null && _sortItems.Count > 0;

        public Expression Filter { get; set; }

        public QueryPurpose Purpose => QueryPurpose.ToDataTable;

        public bool IsOutQuery => false;

        public bool Distinct { get; set; }

        public int TopOrPageSize { get; private set; }

        public int PageIndex { get; private set; }
        #endregion

        internal SqlGroupQuery(ISqlSelectQuery groupFrom)
        {
            GroupFrom = groupFrom;
        }

        public async Task<IList<TResult>> ToListAsync<TResult>(Func<SqlRowReader, TResult> selector,
            params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            //if (PageIndex > -1 && !HasSortItems)
            //    throw new ArgumentException("Paged query must has sort items.");

            //Purpose = QueryPurpose.ToDataTable;

            //TODO:检查SelectItem是否GroupKey or 聚合函数
            for (int i = 0; i < selectItem.Length; i++)
            {
                selectItem[i].Target.Owner = this;
                Selects.Add(selectItem[i].Target.AliasName, selectItem[i].Target);
            }

            throw new NotImplementedException();
        }

    }
}
