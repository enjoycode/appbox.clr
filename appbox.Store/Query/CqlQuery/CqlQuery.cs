using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using appbox.Caching;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    public struct CqlQuery
    {
        private StringBuilder builder;
        private EntityModel model;
        private bool hasWhere;
        public int Limit;
        public bool AllowFiltering;

        public CqlQuery(ulong entityModelId, string viewName = null)
        {
            model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(entityModelId).Result;
            builder = StringBuilderCache.Acquire();
            builder.Append(" FROM \"");
            if (string.IsNullOrEmpty(viewName))
                builder.Append(model.Name);
            else
                builder.Append($"{model.Id}_{viewName}");

            builder.Append("\" ");
            hasWhere = false;
            AllowFiltering = false;
            Limit = 1000;
        }

        /// <summary>
        /// 过滤条件
        /// </summary>
        /// <param name="expression">已经转换好的条件表达式</param>
        public void Where(string expression)
        {
            BuildWhere();
            builder.Append(expression);
        }

        public void In<T>(string field, IEnumerable<T> values)
        {
            BuildWhere();
            builder.Append(field);
            builder.Append(" IN (");
            bool first = true;
            foreach (var value in values)
            {
                if (first)
                    first = false;
                else
                    builder.Append(',');

                BuildValue(value);
            }
            builder.Append(')');
        }

        #region ====Private help methods====
        private void BuildWhere()
        {
            if (!hasWhere)
            {
                hasWhere = true;
                builder.Append("WHERE ");
            }
            else
            {
                builder.Append(" AND ");
            }
        }

        private void BuildValue<T>(T value)
        {
            if (typeof(T) == typeof(string))
            {
                builder.Append('\'');
                builder.Append(value);
                builder.Append('\'');
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime v = Convert.ToDateTime(value);
                builder.Append((long)(v - new DateTime(1970, 1, 1)).TotalMilliseconds);
            }
            else
            {
                builder.Append(value);
            }
        }
        #endregion

        #region ====ToXXX Methods====
        public async Task<IList<Entity>> ToListAsync()
        {
            builder.Insert(0, "SELECT *");
            if (Limit > 0)
                builder.Append($" LIMIT {Limit}");
            if (AllowFiltering)
                builder.Append(" ALLOW FILTERING");

            var cql = StringBuilderCache.GetStringAndRelease(builder);
            builder = null;
            var db = CqlStore.Get(model.CqlStoreOptions.StoreModelId);
            Log.Debug(cql);
            var rs = await db.ExecuteAsync(cql);
            return rs.ToEntityList(model);
        }

        public async Task<IList<Entity>> ToListByFilterAsync(Func<IRow, bool> filter, int skip = 0, int take = 0)
        {
            builder.Insert(0, "SELECT *");
            if (Limit > 0)
                builder.Append($" LIMIT {Limit}");
            if (AllowFiltering)
                builder.Append(" ALLOW FILTERING");

            var cql = StringBuilderCache.GetStringAndRelease(builder);
            builder = null;
            var db = CqlStore.Get(model.CqlStoreOptions.StoreModelId);
            var rs = await db.ExecuteAsync(cql);
            var list = new List<Entity>();
            int hasSkip = 0;
            int hasTake = 0;
            foreach (var row in rs)
            {
                if (filter(row))
                {
                    if (hasSkip < skip)
                    {
                        hasSkip++;
                    }
                    else
                    {
                        list.Add(row.FetchToEntity(model));
                        if (take > 0)
                        {
                            hasTake++;
                            if (hasTake == take)
                                return list;
                        }
                    }
                }
            }
            return list;
        }

        public async Task<IList<TResult>> ToListAsync<TResult>(Func<IRow, TResult> selector, string fields)
        {
            builder.Insert(0, fields);
            builder.Insert(0, "SELECT ");
            if (Limit > 0)
                builder.Append($" LIMIT {Limit}");
            if (AllowFiltering)
                builder.Append(" ALLOW FILTERING");

            var cql = StringBuilderCache.GetStringAndRelease(builder);
            builder = null;
            var db = CqlStore.Get(model.CqlStoreOptions.StoreModelId);
            var rs = await db.ExecuteAsync(cql);
            return rs.ToList(selector);
        }

        public async Task<IList<TResult>> ToListByFilterAsync<TResult>(Func<IRow, TResult> selector,
            string fields, Func<IRow, bool> filter, int skip = 0, int take = 0)
        {
            builder.Insert(0, fields);
            builder.Insert(0, "SELECT ");
            if (Limit > 0)
                builder.Append($" LIMIT {Limit}");
            if (AllowFiltering)
                builder.Append(" ALLOW FILTERING");

            var cql = StringBuilderCache.GetStringAndRelease(builder);
            builder = null;
            var db = CqlStore.Get(model.CqlStoreOptions.StoreModelId);
            var rs = await db.ExecuteAsync(cql);
            var list = new List<TResult>();
            int hasSkip = 0;
            int hasTake = 0;
            foreach (var row in rs)
            {
                if (filter(row))
                {
                    if (hasSkip < skip)
                    {
                        hasSkip++;
                    }
                    else
                    {
                        list.Add(row.Fetch(selector));
                        if (take > 0)
                        {
                            hasTake++;
                            if (hasTake == take)
                                return list;
                        }
                    }
                }
            }
            return list;
        }
        #endregion

    }
}
