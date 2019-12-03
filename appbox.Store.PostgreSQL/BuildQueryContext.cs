using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using appbox.Caching;
using appbox.Expressions;
using appbox.Models;

namespace appbox.Store
{
    sealed class BuildQueryContext
    {

        #region ====Properties & Fields====

        /// <summary>
        /// 根查询
        /// </summary>
        internal ISqlQuery RootQuery;
        /// <summary>
        /// 当前正在处理的查询
        /// </summary>
        public ISqlQuery CurrentQuery;
        public QueryInfo CurrentQueryInfo;
        internal DbCommand Command;
        //internal bool IsBuildCTESelectItem;

        private int _queryIndex;
        /// <summary>
        /// 查询字典表
        /// </summary>
        internal Dictionary<ISqlQuery, QueryInfo> Queries;
        private Dictionary<SqlQueryBase, Dictionary<string, EntityExpression>> _autoJoins;

        public Dictionary<SqlQueryBase, Dictionary<string, EntityExpression>> AutoJoins
        {
            get
            {
                if (_autoJoins == null)
                    _autoJoins = new Dictionary<SqlQueryBase, Dictionary<string, EntityExpression>>();
                return _autoJoins;
            }
        }

        private int _parameterIndex;
        /// <summary>
        /// 参数字典表
        /// </summary>
        private Dictionary<object, string> Parameters;

        #endregion

        #region ====Ctor====
        public BuildQueryContext(DbCommand command, ISqlQuery root)
        {
            Parameters = new Dictionary<object, string>();

            Command = command;
            RootQuery = root;
            Queries = new Dictionary<ISqlQuery, QueryInfo>();
            ((SqlQueryBase)RootQuery).AliasName = "t";
            Queries.Add(root, new QueryInfo(root));
        }
        #endregion

        #region ====Methods====

        public void SetBuildStep(BuildQueryStep step)
        {
            CurrentQueryInfo.BuildStep = step;
        }

        public void Append(string sql)
        {
            CurrentQueryInfo.Out.Append(sql);
        }

        public void AppendFormat(string sql, params object[] para)
        {
            CurrentQueryInfo.Out.AppendFormat(sql, para);
        }

        public void AppendLine()
        {
            CurrentQueryInfo.Out.AppendLine();
        }

        public void AppendLine(string sql)
        {
            CurrentQueryInfo.Out.AppendLine(sql);
        }

        public void RemoveLastChar()
        {
            StringBuilder sb = CurrentQueryInfo.Out;
            sb.Remove(sb.Length - 1, 1);
        }

        /// <summary>
        /// 根据参数值获取查询参数名称
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetParameterName(object value)
        {
            string pname = null;
            if (!Parameters.TryGetValue(value, out pname))
            {
                _parameterIndex += 1;
                pname = string.Format("p{0}", _parameterIndex.ToString());
                Parameters.Add(value, pname);

                DbParameter para = Command.CreateParameter();
                para.ParameterName = pname;
                para.Value = value;
                if (value is string)
                    para.Size = ((string)value).Length;
                Command.Parameters.Add(para);
            }

            return pname;
        }

        public void BeginBuildQuery(ISqlQuery query)
        {
            QueryInfo qi = null;

            //尚未处理过，则新建相应的QueryInfo并加入字典表
            //注意：根查询在构造函数时已加入字典表
            if (!Queries.TryGetValue(query, out qi))
                qi = AddSubQuery(query);

            //设置上级的查询及相应的查询信息
            if (!ReferenceEquals(query, this.RootQuery))
            {
                qi.ParentQuery = this.CurrentQuery;
                qi.ParentInfo = this.CurrentQueryInfo;
            }
            //设置当前的查询及相应的查询信息
            CurrentQuery = query;
            CurrentQueryInfo = qi;

            //添加手工联接
            LoopAddQueryJoins((SqlQueryBase)query);
        }

        public void EndBuildQuery(ISqlQuery query)
        {
            //判断是否根查询
            if (ReferenceEquals(CurrentQuery, RootQuery))
            {
                Command.CommandText = CurrentQueryInfo.GetCommandText();
            }
            else
            {
                CurrentQueryInfo.EndBuidSubQuery();
                CurrentQuery = CurrentQueryInfo.ParentQuery;
                CurrentQueryInfo = CurrentQueryInfo.ParentInfo;
            }
        }

        /// <summary>
        /// 添加指定的子查询至查询字典表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private QueryInfo AddSubQuery(ISqlQuery query)
        {
            //先判断是否已存在于手工Join里，如果不存在则需要设置别名
            var q = (SqlQueryBase)query;
            if (!AutoJoins.ContainsKey(q))
            {
                _queryIndex += 1;
                ((SqlQueryBase)query).AliasName = $"t{_queryIndex.ToString()}";
            }
            QueryInfo info = new QueryInfo(query, CurrentQueryInfo);
            Queries.Add(query, info);
            return info;
        }

        /// <summary>
        /// 获取查询的别名
        /// 如果上下文中尚未存在查询，则自动设置别名并加入查询字典表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public string GetQueryAliasName(ISqlQuery query)
        {
            QueryInfo qi = null;
            if (!Queries.TryGetValue(query, out qi))
                qi = AddSubQuery(query); // 添加时会设置别名

            return ((SqlQueryBase)query).AliasName;
        }

        private void LoopAddQueryJoins(SqlQueryBase query)
        {
            //判断是否已经生成别名
            if (string.IsNullOrEmpty(query.AliasName))
            {
                _queryIndex += 1;
                query.AliasName = $"t{_queryIndex.ToString()}";
            }

            //将当前查询加入自动联接字典表
            AutoJoins.Add(query, new Dictionary<string, EntityExpression>());

            if (query.HasJoins)
            {
                foreach (var item in query.Joins)
                {
                    if (item.Right is SqlQueryJoin) //注意：子查询不具备自动联接
                        LoopAddQueryJoins((SqlQueryBase)item.Right);
                    else
                        LoopAddSubQueryJoins((SqlSubQuery)item.Right);
                }
            }
        }

        private void LoopAddSubQueryJoins(SqlSubQuery query)
        {
            if (query.HasJoins)
            {
                foreach (var item in query.Joins)
                {
                    if (item.Right is SqlQueryJoin) //注意：子查询不具备自动联接
                        LoopAddQueryJoins((SqlQueryBase)item.Right);
                    else
                        LoopAddSubQueryJoins((SqlSubQuery)item.Right);
                }
            }
        }

        public string GetEntityRefAliasName(EntityExpression exp, SqlQueryBase query)
        {
            string path = exp.ToString();
            Dictionary<string, EntityExpression> ds = this.AutoJoins[query];

            EntityExpression e = null;
            if (!ds.TryGetValue(path, out e))
            {
                ds.Add(path, exp);
                _queryIndex += 1;
                exp.AliasName = $"j{_queryIndex.ToString()}";
                e = exp;
            }
            return e.AliasName;
        }

        /// <summary>
        /// 用于生成EntityRef的自动Join
        /// </summary>
        public void BuildQueryAutoJoins(SqlQueryBase target)
        {
            if (!AutoJoins.TryGetValue(target, out Dictionary<string, EntityExpression> ds))
                return;

            foreach (var rq in ds.Values)
            {
                //Left Join "City" c ON c."Code" = t."CityCode"
                //eg: Customer.City的City
                var rqModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.ModelID).Result;
                //eg: Customer.City的Customer
                var rqOwnerModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.Owner.ModelID).Result;
                AppendFormat(" Left Join \"{0}\" {1} On ", rqModel.SqlTableName, rq.AliasName);
                //Build ON Condition, other.pks == this.fks
                var rm = (EntityRefModel)rqOwnerModel.GetMember(rq.Name, true);
                for (int i = 0; i < rqModel.SqlStoreOptions.PrimaryKeys.Count; i++)
                {
                    if (i != 0) Append(" And ");
                    var pk = (DataFieldModel)rqModel.GetMember(rqModel.SqlStoreOptions.PrimaryKeys[i].MemberId, true);
                    var fk = (DataFieldModel)rqOwnerModel.GetMember(rm.FKMemberIds[i], true);
                    AppendFormat("{0}.\"{1}\"={2}.\"{3}\"", rq.AliasName, pk.SqlColName, rq.Owner.AliasName, fk.SqlColName);
                }
            }
        }
        #endregion

    }

    sealed class QueryInfo
    {
        #region ====Properties====
        private StringBuilder sb;
        private StringBuilder sb2; //用于输出Where条件

        /// <summary>
        /// 当前正在处理的查询的步骤
        /// </summary>
        public BuildQueryStep BuildStep;

        public StringBuilder Out
        {
            get
            {
                if (BuildStep == BuildQueryStep.BuildWhere
                    || BuildStep == BuildQueryStep.BuildOrderBy
                    || BuildStep == BuildQueryStep.BuildPageTail)
                    return sb2;
                return sb;
            }
        }

        internal ISqlQuery Owner { get; }

        internal ISqlQuery ParentQuery { get; set; }

        internal QueryInfo ParentInfo { get; set; }
        #endregion

        #region ====ctor====
        /// <summary>
        /// 构造根查询信息
        /// </summary>
        public QueryInfo(ISqlQuery owner)
        {
            Owner = owner;
            sb = StringBuilderCache.Acquire();
            sb2 = StringBuilderCache.Acquire();
        }

        /// <summary>
        /// 构造子查询信息
        /// </summary>
        /// <param name="parentInfo"></param>
        public QueryInfo(ISqlQuery owner, QueryInfo parentInfo)
        {
            Owner = owner;
            if (parentInfo.BuildStep == BuildQueryStep.BuildWhere)
                sb = parentInfo.sb2;
            else
                sb = parentInfo.sb;
            sb2 = StringBuilderCache.Acquire();
        }
        #endregion

        #region ====Methods====
        internal void EndBuidSubQuery()
        {
            sb.Append(StringBuilderCache.GetStringAndRelease(sb2));
        }

        internal string GetCommandText()
        {
            sb.Append(StringBuilderCache.GetStringAndRelease(sb2));
            return StringBuilderCache.GetStringAndRelease(sb);
        }
        #endregion

    }

    enum BuildQueryStep : byte
    {
        BuildSelect,
        BuildFrom,
        BuildJoin,
        BuildWhere,
        BuildGroupBy,
        BuildOrderBy,
        BuildUpdateSet,
        BuildUpsertSet,
        BuildWithCTE,
        BuildPageTail,
        BuildPageOrderBy
    }
}
