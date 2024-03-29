﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Expressions;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Store
{
    public sealed class SqlQuery : SqlQueryBase, ISqlSelectQuery
    {

        #region ====Fields & Properties====
        private Dictionary<string, SqlSelectItemExpression> _selects;
        private List<SqlSortItem> _sortItems;

        /// <summary>
        /// Query Target
        /// </summary>
        public EntityExpression T { get; private set; }

        /// <summary>
        /// 筛选器
        /// </summary>
        public Expression Filter { get; set; }

        /// <summary>
        /// 用于EagerLoad导航属性 
        /// </summary>
        private SqlIncluder _rootIncluder;

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

        public bool HasSortItems => _sortItems != null && _sortItems.Count > 0;

        public QueryPurpose Purpose { get; internal set; }

        public bool Distinct { get; set; }

        #region ----分页查询属性----

        public int TakeSize { get; internal set; }

        public int SkipSize { get; internal set; } = 0;

        #endregion

        #region ----树状查询属性----

        public FieldExpression TreeParentIDMember { get; private set; }

        //public EntitySetExpression TreeSubSetMember { get; private set; }

        #endregion

        #region ----GroupBy属性----
        public SqlSelectItemExpression[] GroupByKeys { get; private set; }

        public Expression HavingFilter { get; private set; }
        #endregion

        #endregion

        #region ====Ctor====
        public SqlQuery(ulong entityModelID)
        {
            T = new EntityExpression(entityModelID, this);
        }

        /// <summary>
        /// 仅用于加载EntitySet
        /// </summary>
        internal SqlQuery(SqlIncluder root)
        {
            T = ((EntitySetExpression)root.Expression).RootEntityExpression;
            T.User = this;
            _rootIncluder = root;
        }
        #endregion

        #region ====Top & Page & Distinct Methods====
        public SqlQuery Take(int rows)
        {
            TakeSize = rows;
            return this;
        }

        public SqlQuery Skip(int rows)
        {
            SkipSize = rows;
            return this;
        }

        public SqlQuery Page(int pageSize, int pageIndex) //TODO:remove it
        {
            TakeSize = pageSize;
            SkipSize = pageIndex * pageSize;
            return this;
        }
        #endregion

        #region ====Include Methods====
        public SqlIncluder Include(Func<EntityExpression, MemberExpression> selector, string alias = null)
        {
            if (_rootIncluder == null) _rootIncluder = new SqlIncluder(T);
            return _rootIncluder.ThenInclude(selector, alias);
        }
        #endregion

        #region ====Select Methods====
        public void AddSelectItem(SqlSelectItemExpression item)
        {
            item.Owner = this;
            Selects.Add(item.AliasName, item);
        }

        public async Task<int> CountAsync()
        {
            Purpose = QueryPurpose.Count;
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return 0;
            return reader.GetInt32(0);
        }

        public async Task<Entity> ToSingleAsync()
        {
            Purpose = QueryPurpose.ToSingleEntity;

            //添加选择项
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            AddAllSelects(this, model, T, null);
            if (_rootIncluder != null)
                await _rootIncluder.AddSelects(this, model);

            //递交查询
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            using var reader = await cmd.ExecuteReaderAsync();
            Entity res = null;
            if (await reader.ReadAsync())
            {
                res = FillEntity(model, reader);
            }
            if (_rootIncluder != null)
                await _rootIncluder.LoadEntitySets(db, res, null); //TODO:fix txn
            return res;
        }

        public T ToScalar<T>(SqlSelectItem expression)
        {
            throw new NotImplementedException();
            //this.Purpose = QueryPurpose.ToScalar;
            //this.AddSelectItem(expression.Target);

            //var db = SqlStore.Get(this.StoreName);
            //var cmd = db.DbCommandBuilder.CreateQueryCommand(this);
            //return (T)db.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 动态查询，返回匿名类列表
        /// </summary>
        public async Task<IList<TResult>> ToListAsync<TResult>(Func<SqlRowReader, TResult> selector,
            params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");
            //if (SkipSize > -1 && !HasSortItems)
            //    throw new ArgumentException("Paged query must has sort items."); //TODO:加入默认主键排序

            Purpose = QueryPurpose.ToDataTable;

            if (_selects != null)
                _selects.Clear();
            for (int i = 0; i < selectItem.Length; i++)
            {
                AddSelectItem(selectItem[i].Target);
            }

            //递交查询
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            using var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            var list = new List<TResult>();
            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                SqlRowReader rr = new SqlRowReader(reader);
                while (await reader.ReadAsync())
                {
                    list.Add(selector(rr));
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            return list;
        }

        public async Task<EntityList> ToListAsync()
        {
            Purpose = QueryPurpose.ToEntityList;

            //添加选择项
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            AddAllSelects(this, model, T, null);
            if (_rootIncluder != null)
                await _rootIncluder.AddSelects(this, model);

            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var list = new EntityList(T.ModelID);

            using var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(FillEntity(model, reader));
            }

            if (_rootIncluder != null && list != null)
                await _rootIncluder.LoadEntitySets(db, list, null); //TODO:fix txn
            return list;
        }

        /// <summary>
        /// 返回树状结构的实体集合
        /// </summary>
        /// <param name="childrenMember">例:q.T["SubItems"]</param>
        /// <returns></returns>
        public async Task<EntityList> ToTreeListAsync(MemberExpression childrenMember)
        {
            //TODO:目前实现仅支持单一主键且为Guid的树状结构
            Debug.Assert(ReferenceEquals(childrenMember.Owner, T));
            var children = (EntitySetExpression)childrenMember;
            EntityModel model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            EntitySetModel childrenModel = (EntitySetModel)model.GetMember(children.Name, true);
            EntityRefModel parentModel = (EntityRefModel)model.GetMember(childrenModel.RefMemberId, true);
            DataFieldModel parentIdModel = (DataFieldModel)model.GetMember(parentModel.FKMemberIds[0], true);
            TreeParentIDMember = (FieldExpression)T[parentIdModel.Name];
            var pk = model.SqlStoreOptions.PrimaryKeys[0].MemberId;

            AddAllSelects(this, model, T, null);

            //TODO:加入自动排序
            //if (!string.IsNullOrEmpty(setmodel.RefRowNumberMemberName))
            //{
            //    SqlSortItem sort = new SqlSortItem(T[setmodel.RefRowNumberMemberName], SortType.ASC);
            //    SortItems.Insert(0, sort);
            //}

            //如果没有设置任何条件，则设置默认条件为查询根级开始
            if (Equals(null, Filter))
                Filter = TreeParentIDMember == null;

            Purpose = QueryPurpose.ToEntityTreeList;
            EntityList list = new EntityList(childrenModel);
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var dic = new Dictionary<Guid, Entity>(); //TODO: fix pk

            using var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Entity obj = FillEntity(model, reader);
                //设置obj本身的EntitySet成员为已加载，防止从数据库中再次加载
                obj.InitEntitySetForLoad(childrenModel);

                var parentId = obj.GetGuidNullable(parentIdModel.MemberId);
                if (parentId.HasValue && dic.TryGetValue(parentId.Value, out Entity parent))
                    parent.GetEntitySet(childrenModel.MemberId).Add(obj);
                else
                    list.Add(obj);

                dic.Add(obj.GetGuid(pk), obj);
            }
            return list;
        }

        /// <summary>
        /// To the tree node path.
        /// </summary>
        /// <returns>注意：可能返回Null</returns>
        /// <param name="parentMember">Parent member.</param>
        public async Task<TreeNodePath> ToTreeNodePathAsync(MemberExpression parentMember, Expression displayText)
        {
            //TODO:目前实现仅支持单一主键且为Guid的树状结构
            //TODO:验证parentMember为EntityExpression,且非聚合引用，且引用目标为自身
            var parent = parentMember as EntityExpression;
            if (Expression.IsNull(parent))
                throw new ArgumentException("parentMember must be EntityRef", nameof(parentMember));

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            var parentModel = (EntityRefModel)model.GetMember(parent.Name, true);
            if (parentModel.IsAggregationRef)
                throw new ArgumentException("can not be AggregationRef", nameof(parentMember));
            if (parentModel.RefModelIds[0] != model.Id)
                throw new ArgumentException("must be self-ref", nameof(parentMember));
            var pkName = model.GetMember(model.SqlStoreOptions.PrimaryKeys[0].MemberId, true).Name;
            var fkName = model.GetMember(parentModel.FKMemberIds[0], true).Name;

            Purpose = QueryPurpose.ToTreeNodePath;
            AddSelectItem(new SqlSelectItemExpression(T[pkName]));
            AddSelectItem(new SqlSelectItemExpression(T[fkName], "ParentId"));
            AddSelectItem(new SqlSelectItemExpression(displayText, "Text"));

            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            using var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;
            Log.Debug(cmd.CommandText);

            var list = new List<TreeNodeInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TreeNodeInfo() { ID = reader.GetGuid(0), Text = reader.GetString(2) });
            }

            return new TreeNodePath(list);
        }

        /// <summary>
        /// 将查询行转换为实体实例
        /// </summary>
        internal static Entity FillEntity(EntityModel model, DbDataReader reader)
        {
            Entity obj = new Entity(model, true);
            //填充实体成员
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string path = reader.GetName(i);
                if (path != SqlStore.TotalRowsColumnName
                    && path != SqlStore.RowNumberColumnName) //注意：过滤查询时的特殊附加列
                {
                    FillMemberValue(obj, path, reader, i);
                }
            }

            //不需要obj.AcceptChanges()，新建时已处理持久状态
            return obj;
        }

        /// <summary>
        /// 根据成员路径填充相应的成员的值
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="path">eg: Order.Customer.ID or Name</param>
        private static void FillMemberValue(Entity target, ReadOnlySpan<char> path, DbDataReader reader, int clIndex)
        {
            if (reader.IsDBNull(clIndex)) //null直接跳过
                return;

            var indexOfDot = path.IndexOf('.');
            if (indexOfDot < 0)
            {
                ref EntityMember m = ref target.TryGetMember(path, out bool found);
                if (!found) //不存在的作为附加成员
                {
                    target.AddAttached(path.ToString(), reader.GetValue(clIndex));
                }
                else
                {
                    //Log.Warn($"Fill {target.Model.Name}.{path.ToString()} value={reader.GetValue(clIndex).ToString()}");
                    m.Flag.HasLoad = m.Flag.HasValue = true;
                    switch (m.ValueType)
                    {
                        case EntityFieldType.String: m.ObjectValue = reader.GetString(clIndex); break;
                        case EntityFieldType.Binary: m.ObjectValue = (byte[])reader.GetValue(clIndex); break;
                        case EntityFieldType.Guid: m.GuidValue = reader.GetGuid(clIndex); break;
                        case EntityFieldType.Decimal: m.DecimalValue = reader.GetDecimal(clIndex); break;
                        case EntityFieldType.DateTime: m.DateTimeValue = reader.GetDateTime(clIndex); break;
                        case EntityFieldType.Double: m.DoubleValue = reader.GetDouble(clIndex); break;
                        case EntityFieldType.Float: m.FloatValue = reader.GetFloat(clIndex); break;
                        case EntityFieldType.Enum: m.Int32Value = reader.GetInt32(clIndex); break;
                        case EntityFieldType.Int64: m.Int64Value = reader.GetInt64(clIndex); break;
                        case EntityFieldType.UInt64: m.UInt64Value = unchecked((ulong)reader.GetInt64(clIndex)); break;
                        case EntityFieldType.Int32: m.Int32Value = reader.GetInt32(clIndex); break;
                        case EntityFieldType.UInt32: m.UInt32Value = unchecked((uint)reader.GetInt32(clIndex)); break;
                        case EntityFieldType.Int16: m.Int16Value = reader.GetInt16(clIndex); break;
                        case EntityFieldType.UInt16: m.UInt16Value = unchecked((ushort)reader.GetInt16(clIndex)); break;
                        case EntityFieldType.Byte: m.ByteValue = reader.GetByte(clIndex); break;
                        case EntityFieldType.Boolean: m.BooleanValue = reader.GetBoolean(clIndex); break;
                        default: throw new NotSupportedException(m.ValueType.ToString());
                    }
                }
            }
            else
            {
                var name = path.Slice(0, indexOfDot);
                var mm = (EntityRefModel)target.Model.GetMember(name, true);
                if (mm.IsAggregationRef)
                    throw new NotImplementedException();
                ref EntityMember m = ref target.GetMember(mm.MemberId);
                if (m.ObjectValue == null) //没有初始化
                {
                    var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(mm.RefModelIds[0]).Result;
                    var entityRef = new Entity(model, true);
                    entityRef.Parent = target;
                    m.Flag.HasLoad = m.Flag.HasValue = true;
                    m.ObjectValue = entityRef;
                    //TODO: 反向引用初始化
                }
                FillMemberValue((Entity)m.ObjectValue, path.Slice(indexOfDot + 1), reader, clIndex);
            }
        }

        internal static void AddAllSelects(SqlQuery query, EntityModel model, EntityExpression T, string fullPath)
        {
            //TODO:考虑特殊SqlSelectItemExpression with *，但只能在fullpath==null时使用
            var members = model.Members;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Type == EntityMemberType.DataField
                    /*|| members[i].Type == EntityMemberType.Aggregate
                    || members[i].Type == EntityMemberType.Formula
                    || members[i].Type == EntityMemberType.AutoNumber
                    || members[i].Type == EntityMemberType.AggregationRefField*/)
                {
                    string alias = fullPath == null ? members[i].Name : $"{fullPath}.{members[i].Name}";
                    SqlSelectItemExpression si = new SqlSelectItemExpression(T[members[i].Name], alias);
                    query.AddSelectItem(si);
                }
            }
        }
        #endregion

        #region ====AsXXX Methods====
        public SqlSubQuery AsSubQuery(params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            foreach (var item in selectItem)
            {
                AddSelectItem(item.Target);
            }

            return new SqlSubQuery(this);
        }

        public SqlFromQuery AsFromQuery(params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            foreach (var item in selectItem)
            {
                AddSelectItem(item.Target);
            }

            return new SqlFromQuery(this);
        }
        #endregion

        #region ====Where Methods====
        public SqlQuery Where(Expression condition)
        {
            Filter = condition;
            return this;
        }

        public SqlQuery AndWhere(Expression condition)
        {
            if (Expression.IsNull(Filter))
                Filter = condition;
            else
                Filter = new BinaryExpression(Filter, condition, BinaryOperatorType.AndAlso);
            return this;
        }

        public SqlQuery OrWhere(Expression conditin)
        {
            if (Expression.IsNull(Filter))
                Filter = conditin;
            else
                Filter = new BinaryExpression(Filter, conditin, BinaryOperatorType.OrElse);
            return this;
        }
        #endregion

        #region ====OrderBy Methods====
        public SqlQuery OrderBy(Expression sortItem)
        {
            SqlSortItem sort = new SqlSortItem(sortItem, SortType.ASC);
            SortItems.Add(sort);
            return this;
        }

        public SqlQuery OrderByDesc(Expression sortItem)
        {
            SqlSortItem sort = new SqlSortItem(sortItem, SortType.DESC);
            SortItems.Add(sort);
            return this;
        }
        #endregion

        #region ====GroupBy Methods====
        public SqlQuery GroupBy(params SqlSelectItem[] groupKeys)
        {
            if (groupKeys == null || groupKeys.Length <= 0)
                throw new ArgumentException("must select some one");

            GroupByKeys = new SqlSelectItemExpression[groupKeys.Length];
            for (int i = 0; i < GroupByKeys.Length; i++)
            {
                groupKeys[i].Target.Owner = this;
                GroupByKeys[i] = groupKeys[i].Target;
            }

            return this;
        }

        public SqlQuery Having(Expression condition)
        {
            HavingFilter = condition;
            return this;
        }
        #endregion
    }
}
