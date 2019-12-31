using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using appbox.Expressions;
using appbox.Models;
using Npgsql;

namespace appbox.Store
{
    partial class PgSqlStore
    {
        protected override DbCommand BuildQuery(ISqlSelectQuery query)
        {
            var cmd = new NpgsqlCommand();
            var ctx = new BuildQueryContext(cmd, query);

            //if (query.Purpose == QueryPurpose.ToEntityTreeList)
            //    BuildTreeQuery(query, ctx);
            //else
            if (query.Purpose == QueryPurpose.ToTreeNodePath)
                BuildTreeNodePathQuery(query, ctx);
            else
                BuildNormalQuery(query, ctx);
            return cmd;
        }

        private void BuildNormalQuery(ISqlSelectQuery query, BuildQueryContext ctx)
        {
            //设置上下文
            ctx.BeginBuildQuery(query);

            //构建Select
            ctx.Append("Select ");
            if (query.Purpose == QueryPurpose.ToDataTable && query.Distinct)
                ctx.Append("Distinct ");

            //构建Select Items
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildSelect;
            if (query.Purpose == QueryPurpose.Count)
            {
                ctx.Append("Count(*)");
            }
            else
            {
                foreach (var si in query.Selects.Values)
                {
                    BuildSelectItem(si, ctx);
                    ctx.Append(",");
                }
                ctx.RemoveLastChar(); //移除最后多余的,号
            }

            //构建From
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildFrom;
            ctx.Append(" From ");
            //判断From源
            if (query is SqlFromQuery)
            {
                SqlFromQuery q = (SqlFromQuery)ctx.CurrentQuery;
                //开始构建From子查询
                ctx.Append("(");
                BuildNormalQuery(q.Target, ctx);
                ctx.Append(")");
                ctx.AppendFormat(" {0}", ((SqlQueryBase)q.Target).AliasName);// ((QueryBase)query).AliasName);
            }
            else
            {
                SqlQuery q = (SqlQuery)ctx.CurrentQuery;
                var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(q.T.ModelID).Result;
                ctx.AppendFormat("\"{0}\" {1}", model.GetSqlTableName(false, null), q.AliasName);
            }

            //构建Where
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildWhere;
            if (!Equals(null, ctx.CurrentQuery.Filter))
            {
                ctx.Append(" Where ");
                BuildExpression(ctx.CurrentQuery.Filter, ctx);
            }

            //非分组的情况下构建Order By
            if (query.Purpose != QueryPurpose.Count)
            {
                if (query.GroupByKeys == null && query.HasSortItems)
                {
                    ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildOrderBy;
                    BuildOrderBy(query, ctx);
                }
            }

            //构建Join
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildJoin;
            SqlQueryBase q1 = (SqlQueryBase)ctx.CurrentQuery;
            if (q1.HasJoins) //先处理每个手工的联接及每个手工联接相应的自动联接
            {
                BuildJoins(q1.Joins, ctx);
            }
            ctx.BuildQueryAutoJoins(q1); //再处理自动联接

            //处理Skip and Take
            if (query.Purpose != QueryPurpose.Count)
            {
                ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildSkipAndTake;
                if (query.SkipSize > 0)
                    ctx.AppendFormat(" Offset {0}", query.SkipSize);
                if (query.Purpose == QueryPurpose.ToSingleEntity)
                    ctx.Append(" Limit 1 ");
                else if (query.TakeSize > 0)
                    ctx.AppendFormat(" Limit {0} ", query.TakeSize);
            }

            //构建分组、Having及排序
            BuildGroupBy(query, ctx);

            //结束上下文
            ctx.EndBuildQuery(query);
        }

        private void BuildTreeNodePathQuery(ISqlSelectQuery query, BuildQueryContext ctx)
        {
            //设置上下文
            ctx.BeginBuildQuery(query);

            ctx.Append("With RECURSIVE cte (\"Id\",\"ParentId\",\"Text\",\"Level\") As (Select ");
            //Select Anchor
            ctx.SetBuildStep(BuildQueryStep.BuildSelect);
            BuildCTE_SelectItems(query, ctx, true);
            ctx.Append("0 From ");
            //From Anchor
            ctx.SetBuildStep(BuildQueryStep.BuildFrom);
            SqlQuery q = (SqlQuery)query;
            var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(q.T.ModelID).Result;
            ctx.AppendFormat("\"{0}\" As {1}", model.GetSqlTableName(false, null), q.AliasName);
            //Where Anchor
            ctx.SetBuildStep(BuildQueryStep.BuildWhere);
            if (!Equals(null, query.Filter))
            {
                ctx.Append(" Where ");
                BuildExpression(query.Filter, ctx);
            }
            //End 1
            ctx.EndBuildQuery(query);

            //Union all
            ctx.SetBuildStep(BuildQueryStep.BuildSelect);
            ctx.Append(" Union All Select ");
            //Select 2
            //ctx.SetBuildStep(BuildQueryStep.BuildSelect);
            BuildCTE_SelectItems(query, ctx, true);
            ctx.Append("\"Level\" + 1 From ");
            //From 2
            ctx.SetBuildStep(BuildQueryStep.BuildFrom);
            ctx.AppendFormat("\"{0}\" As {1}", model.GetSqlTableName(false, null), q.AliasName);
            //Inner Join 
            ctx.Append(" Inner Join cte as d On d.\"ParentId\"=t.\"Id\" ) Select * From cte");

            //End 1
            ctx.EndBuildQuery(query);
        }

        private void BuildCTE_SelectItems(ISqlSelectQuery query, BuildQueryContext ctx, bool forTeeNodePath = false)
        {
            //ctx.IsBuildCTESelectItem = true;
            foreach (var si in query.Selects.Values)
            {
                FieldExpression fsi = si.Expression as FieldExpression;
                if (!Expression.IsNull(fsi))
                {
                    if (Equals(fsi.Owner.Owner, null))
                    {
                        if (forTeeNodePath)
                            ctx.AppendFormat("t.\"{0}\" \"{1}\",", fsi.Name, si.AliasName);
                        else
                            ctx.AppendFormat("t.\"{0}\",", fsi.Name);
                    }
                }
                //else if (forTeeNodePath)
                //{
                //    var aggRefField = si.Expression as AggregationRefFieldExpression;
                //    if (!object.Equals(null, aggRefField))
                //    {
                //        BuildAggregationRefFieldExpression(aggRefField, ctx);
                //        ctx.AppendFormat(" \"{0}\",", si.AliasName);
                //    }
                //}
            }
            //ctx.IsBuildCTESelectItem = false;
        }

        private void BuildOrderBy(ISqlSelectQuery query, BuildQueryContext ctx)
        {
            ctx.Append(" Order By ");
            for (int i = 0; i < query.SortItems.Count; i++)
            {
                SqlSortItem si = query.SortItems[i];
                BuildExpression(si.Expression, ctx);
                if (si.SortType == SortType.DESC)
                    ctx.Append(" DESC");
                if (i < query.SortItems.Count - 1)
                    ctx.Append(" ,");
            }
        }

        private void BuildGroupBy(ISqlSelectQuery query, BuildQueryContext ctx)
        {
            if (query.GroupByKeys == null || query.GroupByKeys.Length == 0)
                return;

            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildGroupBy;
            ctx.Append(" Group By ");
            for (int i = 0; i < query.GroupByKeys.Length; i++)
            {
                if (i != 0) ctx.Append(",");
                BuildExpression(query.GroupByKeys[i], ctx);
            }
            if (!Expression.IsNull(query.HavingFilter))
            {
                ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildHaving;
                ctx.Append(" Having ");
                BuildExpression(query.HavingFilter, ctx);
            }
            if (query.HasSortItems)
            {
                ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildOrderBy;
                BuildOrderBy(query, ctx);
            }
        }

        private void BuildSelectItem(SqlSelectItemExpression item, BuildQueryContext ctx)
        {
            //判断item.Expression是否是子Select项,是则表示外部查询（FromQuery）引用的Select项
            if (item.Expression.Type == ExpressionType.SelectItemExpression)
            {
                SqlSelectItemExpression si = (SqlSelectItemExpression)item.Expression;
                //判断当前查询是否等于Select项的所有者，否则表示Select项的所有者的外部查询引用该Select项
                if (ReferenceEquals(ctx.CurrentQuery, item.Owner))
                    ctx.AppendFormat("{0}.\"{1}\"", ctx.GetQueryAliasName(si.Owner), si.AliasName);
                else
                    ctx.AppendFormat("{0}.\"{1}\"", ctx.GetQueryAliasName(item.Owner), si.AliasName);

                //处理选择项别名
                if (ctx.CurrentQueryInfo.BuildStep == BuildQueryStep.BuildSelect)//&& !ctx.IsBuildCTESelectItem)
                {
                    if (item.AliasName != si.AliasName)
                        ctx.AppendFormat(" \"{0}\"", item.AliasName);
                }
            }
            else //----上面为FromQuery的Select项，下面为Query或SubQuery的Select项----
            {
                //判断当前查询是否等于Select项的所有者，否则表示Select项的所有者的外部查询引用该Select项
                if (ReferenceEquals(ctx.CurrentQuery, item.Owner))
                    BuildExpression(item.Expression, ctx);
                else
                    ctx.AppendFormat("{0}.\"{1}\"", ctx.GetQueryAliasName(item.Owner), item.AliasName);

                //处理选择项别名
                if (ctx.CurrentQueryInfo.BuildStep == BuildQueryStep.BuildSelect)//&& !ctx.IsBuildCTESelectItem)
                {
                    MemberExpression memberExp = item.Expression as MemberExpression;
                    if (Expression.IsNull(memberExp)
                        /*|| memberExp.Type == ExpressionType.AggregationRefFieldExpression*/ //注意：聚合引用字段必须用别名
                        || memberExp.Name != item.AliasName)
                    {
                        ctx.AppendFormat(" \"{0}\"", item.AliasName);
                    }
                }
            }
        }

        /// <summary>
        /// 处理手工联接及其对应的自动联接
        /// </summary>
        private void BuildJoins(List<SqlJoin> joins, BuildQueryContext ctx)
        {
            foreach (var item in joins)
            {
                //先处理当前的联接
                ctx.Append(GetJoinString(item.JoinType));
                if (item.Right is SqlQueryJoin)
                {
                    var j = (SqlQueryJoin)item.Right;
                    var jModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(j.T.ModelID).Result;
                    ctx.AppendFormat("\"{0}\" {1} On ", jModel.GetSqlTableName(false, null), j.AliasName);
                    BuildExpression(item.OnConditon, ctx);

                    //再处理手工联接的自动联接
                    ctx.BuildQueryAutoJoins(j);
                }
                else //否则表示联接对象是SubQuery，注意：子查询没有自动联接
                {
                    SqlSubQuery sq = (SqlSubQuery)item.Right;
                    ctx.Append("(");
                    BuildNormalQuery(sq.Target, ctx);
                    ctx.AppendFormat(") As {0} On ", ((SqlQueryBase)sq.Target).AliasName);
                    BuildExpression(item.OnConditon, ctx);
                }

                //最后递归当前联接的右部是否还有手工的联接项
                if (item.Right.HasJoins)
                    BuildJoins(item.Right.Joins, ctx);
            }
        }

        #region ====Build Expression Methods====
        private void BuildExpression(Expression exp, BuildQueryContext ctx)
        {
            switch (exp.Type)
            {
                case ExpressionType.FieldExpression:
                    BuildFieldExpression((FieldExpression)exp, ctx);
                    break;
                case ExpressionType.EntityExpression:
                    BuildEntityExpression((EntityExpression)exp, ctx);
                    break;
                //case ExpressionType.AggregationRefFieldExpression:
                //    BuildAggregationRefFieldExpression((AggregationRefFieldExpression)exp, ctx);
                //    break;
                case ExpressionType.BinaryExpression:
                    BuildBinaryExpression((BinaryExpression)exp, ctx);
                    break;
                case ExpressionType.PrimitiveExpression:
                    BuildPrimitiveExpression((PrimitiveExpression)exp, ctx);
                    break;
                case ExpressionType.SelectItemExpression:
                    BuildSelectItem((SqlSelectItemExpression)exp, ctx);
                    break;
                case ExpressionType.SubQueryExpression:
                    BuildSubQuery((SqlSubQuery)exp, ctx);
                    break;
                ////case ExpressionType.DbParameterExpression:
                ////    BuildDbParameterExpression((DbParameterExpression)exp, ctx);
                ////    break;
                case ExpressionType.DbFuncExpression:
                    BuidDbFuncExpression((DbFuncExpression)exp, ctx);
                    break;
                case ExpressionType.DbParameterExpression:
                    BuildDbParameterExpression((DbParameterExpression)exp, ctx);
                    break;
                //case ExpressionType.InvocationExpression:
                //    BuildInvocationExpression((InvocationExpression)exp, ctx);
                //    break;
                default:
                    throw new NotSupportedException($"Not Supported Expression Type [{exp.Type.ToString()}] for Query.");
            }
        }

        private void BuildSubQuery(SqlSubQuery exp, BuildQueryContext ctx)
        {
            ctx.Append("(");
            BuildNormalQuery(exp.Target, ctx);
            ctx.Append(")");
        }

        private void BuildPrimitiveExpression(PrimitiveExpression exp, BuildQueryContext ctx)
        {
            if (exp.Value == null)
            {
                ctx.Append("NULL");
                return;
            }

            if (exp.Value is IEnumerable list && !(exp.Value is string)) //用于处理In及NotIn的参数
            {
                ctx.Append("(");
                bool first = true;
                foreach (var item in list)
                {
                    if (first) first = false;
                    else ctx.Append(",");
                    ctx.AppendFormat("@{0}", ctx.GetParameterName(item));
                }
                ctx.Append(")");
            }
            else
            {
                ctx.AppendFormat("@{0}", ctx.GetParameterName(exp.Value));
            }
        }

        private void BuildEntityExpression(EntityExpression exp, BuildQueryContext ctx)
        {
            //判断是否已处理过
            if (exp.AliasName != null)
                return;

            //处理EntityExpression的IsInDesign属性，全部更改为非设计状态
            //因为客户端模型设计器保存的表达式是设计状态的
            //exp.SetNotInDesignForBuildQuery();

            //判断是否已到达根
            if (Equals(null, exp.Owner))
            {
                //判断exp.User是否为Null，因为可能是附加的QuerySelectItem
                if (exp.User == null)
                {
                    SqlQueryBase q = ctx.CurrentQuery as SqlQueryBase;
                    exp.User = q ?? throw new Exception("NpgsqlCommandHelper.BuildEntityExpression()");
                }
                exp.AliasName = ((SqlQueryBase)exp.User).AliasName;
            }
            else //否则表示自动联接
            {
                //先处理Owner
                BuildEntityExpression(exp.Owner, ctx);
                //再获取自动联接的别名
                exp.AliasName = ctx.GetEntityRefAliasName(exp, (SqlQueryBase)exp.User);
            }
        }

        private void BuildFieldExpression(FieldExpression exp, BuildQueryContext ctx)
        {
            var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(exp.Owner.ModelID).Result;

            //判断上下文是否在处理Update的Set
            if (ctx.CurrentQueryInfo.BuildStep == BuildQueryStep.BuildUpdateSet)
                ctx.AppendFormat("\"{0}\"", exp.Name);
            else if (ctx.CurrentQueryInfo.BuildStep == BuildQueryStep.BuildUpsertSet)
                ctx.AppendFormat("\"{0}\".\"{1}\"", model.Name, exp.Name);
            else
            {
                BuildEntityExpression(exp.Owner, ctx);
                ctx.AppendFormat("{0}.\"{1}\"", exp.Owner.AliasName, exp.Name);
            }
        }

        private void BuildBinaryExpression(BinaryExpression exp, BuildQueryContext ctx)
        {
            //左操作数
            BuildExpression(exp.LeftOperand, ctx);

            //判断是否在处理条件中
            if (exp.RightOperand.Type == ExpressionType.PrimitiveExpression
                && ((PrimitiveExpression)exp.RightOperand).Value == null
                && ctx.CurrentQueryInfo.BuildStep == BuildQueryStep.BuildWhere)
            {
                if (exp.BinaryType == BinaryOperatorType.Equal)
                    ctx.Append(" ISNULL");
                else if (exp.BinaryType == BinaryOperatorType.NotEqual)
                    ctx.Append(" NOTNULL");
                else
                    throw new Exception("BuildBinaryExpression Error.");
            }
            else
            {
                //操作符
                BuildBinaryOperatorType(exp, ctx.CurrentQueryInfo.Out);
                //右操作数
                //暂在这里特殊处理Like通配符
                if (exp.BinaryType == BinaryOperatorType.Like)
                {
                    var pattern = exp.RightOperand as PrimitiveExpression;
                    if (!Expression.IsNull(pattern) && pattern.Value is string)
                        pattern.Value = $"%{pattern.Value}%";
                }
                BuildExpression(exp.RightOperand, ctx);
            }
        }

        private void BuidDbFuncExpression(DbFuncExpression exp, BuildQueryContext ctx)
        {
            ctx.Append($"{exp.Name.ToString()}(");
            if (exp.Parameters != null)
            {
                for (int i = 0; i < exp.Parameters.Length; i++)
                {
                    if (i != 0) ctx.Append(",");
                    BuildExpression(exp.Parameters[i], ctx);
                }
            }
            ctx.Append(")");
        }

        private void BuildDbParameterExpression(DbParameterExpression exp, BuildQueryContext ctx)
        {
            ctx.AppendFormat("@{0}", ctx.GetDbParameterName());
        }
        #endregion

        #region ====private static help methods====
        private static string GetJoinString(JoinType joinType)
        {
            return joinType switch
            {
                JoinType.InnerJoin => " Join ",
                JoinType.LeftJoin => " Left Join ",
                JoinType.RightJoin => " Right Join ",
                JoinType.FullJoin => " Full Join ",
                _ => throw new NotSupportedException(),
            };
        }

        private static void BuildBinaryOperatorType(BinaryExpression exp, StringBuilder sb)
        {
            switch (exp.BinaryType)
            {
                case BinaryOperatorType.AndAlso:
                    sb.Append(" And ");
                    break;
                case BinaryOperatorType.OrElse:
                    sb.Append(" Or ");
                    break;
                case BinaryOperatorType.BitwiseAnd:
                    sb.Append(" & ");
                    break;
                case BinaryOperatorType.BitwiseOr:
                    sb.Append(" | ");
                    break;
                case BinaryOperatorType.BitwiseXor:
                    sb.Append(" ^ ");
                    break;
                case BinaryOperatorType.Divide:
                    sb.Append(" / ");
                    break;
                case BinaryOperatorType.Assign:
                case BinaryOperatorType.Equal:
                    sb.Append(" = ");
                    break;
                case BinaryOperatorType.Greater:
                    sb.Append(" > ");
                    break;
                case BinaryOperatorType.GreaterOrEqual:
                    sb.Append(" >= ");
                    break;
                case BinaryOperatorType.In:
                    sb.Append(" In ");
                    break;
                case BinaryOperatorType.NotIn:
                    sb.Append(" Not In ");
                    break;
                case BinaryOperatorType.Is:
                    sb.Append(" Is ");
                    break;
                case BinaryOperatorType.IsNot:
                    sb.Append(" Is Not ");
                    break;
                case BinaryOperatorType.Less:
                    sb.Append(" < ");
                    break;
                case BinaryOperatorType.LessOrEqual:
                    sb.Append(" <= ");
                    break;
                case BinaryOperatorType.Like:
                    sb.Append(" Like ");
                    break;
                case BinaryOperatorType.Minus:
                    sb.Append(" - ");
                    break;
                case BinaryOperatorType.Modulo:
                    break;
                case BinaryOperatorType.Multiply:
                    sb.Append(" * ");
                    break;
                case BinaryOperatorType.NotEqual:
                    sb.Append(" <> ");
                    break;
                case BinaryOperatorType.Plus:
                    if (CheckNeedConvertStringAddOperator(exp))
                        sb.Append(" || ");
                    else
                        sb.Append(" + ");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 用于字符串+连接时转换为||操作符
        /// </summary>
        /// <returns>true需要转换</returns>
        private static bool CheckNeedConvertStringAddOperator(Expression exp)
        {
            switch (exp.Type)
            {
                case ExpressionType.BinaryExpression:
                    {
                        var e = (BinaryExpression)exp;
                        return CheckNeedConvertStringAddOperator(e.LeftOperand)
                            || CheckNeedConvertStringAddOperator(e.RightOperand);
                    }
                case ExpressionType.FieldExpression:
                    {
                        var e = (FieldExpression)exp;
                        var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(e.Owner.ModelID).Result;
                        var fieldModel = (DataFieldModel)model.GetMember(e.Name, true);
                        return fieldModel.DataType == EntityFieldType.String;
                    }
                case ExpressionType.PrimitiveExpression:
                    return ((PrimitiveExpression)exp).Value is string;
                //case ExpressionType.InvocationExpression:
                //    throw new NotImplementedException(); //TODO:根据系统函数判断
                default:
                    throw new NotSupportedException("Not Supported Expression Type ["
                        + exp.Type.ToString() + "] for CheckNeedConvertStringAddOperator.");
            }
        }
        #endregion
    }
}
