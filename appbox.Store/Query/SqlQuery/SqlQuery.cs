using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Expressions;
using appbox.Models;

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

        public bool IsOutQuery => false;

        public QueryPurpose Purpose { get; internal set; }

        public bool Distinct { get; set; }

        #region ----分页查询属性----

        public int TopOrPageSize { get; internal set; }

        public int PageIndex { get; internal set; } = -1;

        #endregion

        #region ----树状查询属性----

        public FieldExpression TreeParentIDMember { get; private set; }

        public EntitySetExpression TreeSubSetMember { get; private set; }

        #endregion

        #endregion

        #region ====Ctor====
        public SqlQuery(ulong entityModelID)
        {
            T = new EntityExpression(entityModelID, this);
        }
        #endregion

        #region ====Top & Page & Distinct Methods====
        public SqlQuery Top(int topSize)
        {
            TopOrPageSize = topSize;
            PageIndex = -1;
            return this;
        }

        public SqlQuery Page(int pageSize, int pageIndex)
        {
            TopOrPageSize = pageSize;
            PageIndex = pageIndex;
            return this;
        }
        #endregion

        #region ====Include Methods====
        /// <summary>
        /// 查询时包含EntityRef的显示文本，注意：只支持一级如t.Order，但不支持t.Order.Customer
        /// </summary>
        public SqlQuery IncludeRefDisplayText(MemberExpression entityRef)
        {
            throw new NotImplementedException();
            //var refMember = entityRef as EntityExpression;
            //if (Expression.IsNull(refMember))
            //{
            //    throw new ArgumentException("Must be EntityRef", nameof(entityRef));
            //}
            //if (!ReferenceEquals(refMember.Owner, this.T))
            //{
            //    throw new ArgumentException("Must be current entity's ref", nameof(entityRef));
            //}

            ////todo:考虑防止重复加入
            //var ownerModel = RuntimeContext.Default.EntityModelContainer.GetModel(T.ModelID);
            //var refModel = ownerModel[refMember.Name] as EntityRefModel;
            //return IncludeRefDisplayText(refModel);
        }

        /// <summary>
        /// 仅内部使用
        /// </summary>
        internal SqlQuery IncludeRefDisplayText(EntityRefModel refModel)
        {
            throw new NotImplementedException();
            //if (!refModel.IsAggregationRef)
            //{
            //    //todo: 暂只使用ToStringExpression，待实现EntityRefModel.DisplayText属性后优先使用
            //    var toStringExp = refModel.GetRefModel(0).ToStringExpression;
            //    if (Expression.IsNull(toStringExp))
            //    {
            //        this.AddSelectItem(new SelectItemExpression(this.T[refModel.IDMemberName], refModel.Name + "DisplayText"));
            //    }
            //    else
            //    {
            //        var newExp = ToStringExpressionHelper.ReplaceEntityExpression(toStringExp, (EntityExpression)this.T[refModel.Name]);
            //        this.AddSelectItem(new SelectItemExpression(newExp, refModel.Name + "DisplayText"));
            //    }
            //}

            //return this;
        }
        #endregion

        #region ====Select Methods====

        public void AddSelectItem(SqlSelectItemExpression item)
        {
            item.Owner = this;
            this.Selects.Add(item.AliasName, item);
        }

        //public AppBox.Core.DataTable ToDataTable(params SelectItem[] selectItem)
        //{
        //    if (selectItem == null || selectItem.Length <= 0)
        //        throw new ArgumentException("must select some one");

        //    if (this.PageIndex > -1 && !this.HasSortItems)
        //        throw new ArgumentException("Paged query must has sort items."); //todo:加入默认ID排序

        //    this.Purpose = QueryPurpose.ToDataTable;

        //    if (this._selects != null)
        //        this._selects.Clear();
        //    for (int i = 0; i < selectItem.Length; i++)
        //    {
        //        this.AddSelectItem(selectItem[i].Target);
        //    }

        //    //递交查询
        //    var db = SqlStore.Get(this.StoreName);
        //    var cmd = db.DbCommandBuilder.CreateQueryCommand(this);
        //    return db.ExecuteToTable(cmd);
        //}

        public SqlSubQuery AsSubQuery(params SqlSelectItem[] selectItem)
        {
            if (selectItem == null || selectItem.Length <= 0)
                throw new ArgumentException("must select some one");

            foreach (var item in selectItem)
            {
                this.AddSelectItem(item.Target);
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

        public async Task<Entity> ToSingleAsync()
        {
            Purpose = QueryPurpose.ToSingleEntity;

            //根据模型添加所有选择项
            var model = await Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            AddAllSelects(this, model, T, null);

            //递交查询
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;

            using var reader = await cmd.ExecuteReaderAsync();
            Entity res = null;
            if (await reader.ReadAsync())
            {
                res = FillEntity(model, reader);
            }
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
        /// 动态查询
        /// </summary>
        //public List<TResult> ToList<TResult>(Func<SqlRow, TResult> selector, params SelectItem[] selectItem)
        //{
        //if (selectItem == null || selectItem.Length <= 0)
        //    throw new ArgumentException("must select some one");

        //if (this.PageIndex > -1 && !this.HasSortItems)
        //    throw new ArgumentException("Paged query must has sort items."); //todo:加入默认ID排序

        //this.Purpose = QueryPurpose.ToDataTable;

        //if (this._selects != null)
        //    this._selects.Clear();
        //for (int i = 0; i < selectItem.Length; i++)
        //{
        //    this.AddSelectItem(selectItem[i].Target);
        //}

        ////递交查询
        //var db = SqlStore.Get(this.StoreName);
        //var cmd = db.DbCommandBuilder.CreateQueryCommand(this);
        //var list = new List<TResult>();
        //SqlRow row;
        //db.ExecuteReader(cmd, dr =>
        //{
        //    while (dr.Read())
        //    {
        //        row = new SqlRow(dr);
        //        list.Add(selector(row));
        //    }
        //});
        //return list;
        //}

        public async Task<EntityList> ToListAsync()
        {
            if (PageIndex > -1 && !HasSortItems)
                throw new ArgumentException("Paged query must has sort items.");

            Purpose = QueryPurpose.ToEntityList;

            var list = new EntityList(T.ModelID);
            await ExecToListInternal(list);
            return list;
        }

        /// <summary>
        /// 返回树状结构的实体集合
        /// </summary>
        /// <param name="treeSubSetMember">例:q.T["SubItems"]</param>
        /// <returns></returns>
        public EntityList ToTreeList(MemberExpression treeSubSetMember)
        {
            throw new NotImplementedException();
            //this.TreeSubSetMember = (EntitySetExpression)treeSubSetMember;
            //EntityModel ownerModel = RuntimeContext.Default.EntityModelContainer.GetModel(this.TreeSubSetMember.Owner.ModelID);
            //EntitySetModel setmodel = ownerModel[this.TreeSubSetMember.Name] as EntitySetModel;
            //this.TreeParentIDMember = (FieldExpression)this.T[setmodel.RefIDMemberName];

            ////加入自动排序
            //if (!string.IsNullOrEmpty(setmodel.RefRowNumberMemberName))
            //{
            //    SortItem sort = new SortItem(T[setmodel.RefRowNumberMemberName], SortType.ASC);
            //    this.SortItems.Insert(0, sort);
            //}

            //this.Purpose = QueryPurpose.ToEntityTreeList;
            //EntityList list = new EntityList(setmodel);

            ////如果没有设置任何条件，则设置默认条件为查询根级开始
            //if (object.Equals(null, this.Filter))
            //{
            //    this.Filter = this.TreeParentIDMember == null;
            //}

            //this.ExecToListInternal(list);
            //return list;
        }

        /// <summary>
        /// To the tree node path.
        /// </summary>
        /// <returns>注意：可能返回Null</returns>
        /// <param name="parentMember">Parent member.</param>
        public TreeNodePath ToTreeNodePath(MemberExpression parentMember)
        {
            throw new NotImplementedException();
            ////todo:验证parentMember为EntityExpression,且非聚合引用，且引用目标为自身
            //var refMember = parentMember as EntityExpression;
            //if (Expression.IsNull(refMember))
            //{
            //    throw new ArgumentException("parentMember must be EntityRef", nameof(parentMember));
            //}
            //var entityModel = RuntimeContext.Default.EntityModelContainer.GetModel(T.ModelID);
            //var refModel = (EntityRefModel)entityModel[refMember.Name];
            //if (refModel.IsAggregationRef)
            //{
            //    throw new ArgumentException("can not be AggregationRef", nameof(parentMember));
            //}
            //if (refModel.GetRefModel(0).ID != entityModel.ID)
            //{
            //    throw new ArgumentException("must be self-ref", nameof(parentMember));
            //}

            //this.Purpose = QueryPurpose.ToTreeNodePath;
            //this.AddSelectItem(new SelectItemExpression(T["ID"]));
            //this.AddSelectItem(new SelectItemExpression(T[parentMember.Name + "ID"], "ParentID"));
            //var toStringExp = entityModel.ToStringExpression;
            //if (Expression.IsNull(toStringExp))
            //{
            //    this.AddSelectItem(new SelectItemExpression(this.T["ID"], "Text"));
            //}
            //else
            //{
            //    var newExp = ToStringExpressionHelper.ReplaceEntityExpression(toStringExp, this.T);
            //    this.AddSelectItem(new SelectItemExpression(newExp, "Text"));
            //}

            //var db = SqlStore.Get(this.StoreName);
            //var cmd = db.DbCommandBuilder.CreateQueryCommand(this);
            //var list = new List<TreeNodeInfo>();
            //db.ExecuteReader(cmd, reader =>
            //{
            //    while (reader.Read())
            //    {
            //        list.Add(new TreeNodeInfo() { ID = reader.GetGuid(0), Text = reader.GetString(2) });
            //    }
            //});

            //return new TreeNodePath(list);
        }

        private async Task ExecToListInternal(IList<Entity> list)
        {
            //根据模型添加所有选择项
            var model = await Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(T.ModelID);
            AddAllSelects(this, model, T, null);

            //Dictionary<Guid, Entity> dic = null;
            //if (this.Purpose == QueryPurpose.ToEntityTreeList)
            //    dic = new Dictionary<Guid, Entity>();

            //递交查询
            var db = SqlStore.Get(model.SqlStoreOptions.StoreModelId);
            var cmd = db.BuildQuery(this);
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            cmd.Connection = conn;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Entity obj = FillEntity(model, reader);
                //设置obj本身的EntitySet成员为已加载，防止从数据库中再次加载
                //if (!Equals(null, TreeSubSetMember))
                //    obj.InitEntitySetLoad(TreeSubSetMember.Name);

                if (Purpose == QueryPurpose.ToEntityList)
                {
                    list.Add(obj);
                }
                else //树状结构查询
                {
                    throw new NotImplementedException();
                    //if (obj.HasValue(this.TreeParentIDMember.Name))
                    //{
                    //    var parentRefID = obj.GetGuidValue(this.TreeParentIDMember.Name);
                    //    Entity parent = null;
                    //    if (dic.TryGetValue(parentRefID, out parent))
                    //        parent.GetEntitySetValue(this.TreeSubSetMember.Name).Add(obj);
                    //    else
                    //        list.Add(obj);
                    //}
                    //else
                    //{
                    //    list.Add(obj);
                    //}
                    //dic.Add(obj.ID, obj);
                }
            }
            //注意：查询出来的树状表已经排序过，修改于2013-5-31
        }

        /// <summary>
        /// 将查询行转换为实体实例
        /// </summary>
        internal static Entity FillEntity(EntityModel model, System.Data.Common.DbDataReader reader)
        {
            Entity obj = new Entity(model);
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

            //不需要obj.AcceptChanges();
            return obj;
        }

        /// <summary>
        /// 根据成员路径填充相应的成员的值
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="path">eg: Order.Customer.ID or Name</param>
        private static void FillMemberValue(Entity target, string path,
            System.Data.Common.DbDataReader reader, int clIndex)
        {
            if (path.IndexOf('.') < 0)
            {
                ref EntityMember m = ref target.GetMember(path); //TODO:不存在的作为附加成员
                m.Flag.HasLoad = true;
                m.Flag.HasValue = !reader.IsDBNull(clIndex);
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
                    case EntityFieldType.Int32: m.Int32Value = reader.GetInt32(clIndex); break;
                    case EntityFieldType.Int16: m.Int16Value = reader.GetInt16(clIndex); break;
                    case EntityFieldType.Byte: m.ByteValue = reader.GetByte(clIndex); break;
                    case EntityFieldType.Boolean: m.BooleanValue = reader.GetBoolean(clIndex); break;
                    default: throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotImplementedException();
                //string[] sr = path.Split('.');
                //Entity cur = target;
                //for (int i = 0; i < sr.Length; i++)
                //{
                //    if (i == sr.Length - 1)
                //    {
                //        FillMemberValue(cur, sr[i], reader, clIndex);
                //    }
                //    else
                //    {
                //        //在这里调用引用实体的初始化，防止从数据库加载
                //        var entityRef = cur.InitEntityRefLoad(sr[i]);
                //        if (entityRef == null)
                //            break;
                //        else
                //            cur = entityRef;
                //    }
                //}
            }
        }

        // internal for test
        internal static void AddAllSelects(SqlQuery query, EntityModel model, EntityExpression T, string fullPath)
        {
            //TODO:考虑特殊SqlSelectItemExpression with *
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

    }
}
