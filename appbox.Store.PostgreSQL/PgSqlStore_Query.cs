using System;
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
            //else if (query.Purpose == QueryPurpose.ToTreeNodePath)
            //    BuildTreeNodePathQuery(query, ctx);
            //else
            BuildQuery(query, ctx);

            return cmd;
        }

        private void BuildQuery(ISqlSelectQuery query, BuildQueryContext ctx)
        {
            //设置上下文
            ctx.BeginBuildQuery(query);

            //判断是否分页
            if (query.PageIndex > -1)
                ctx.Append("With tt As (");

            //构建Select
            ctx.Append("Select ");
            if (query.Purpose == QueryPurpose.ToDataTable && query.Distinct)
                ctx.Append("Distinct ");

            //构建Select Items
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildSelect;
            foreach (var si in query.Selects.Values)
            {
                BuildSelectItem(si, ctx);
                ctx.Append(",");
            }
            if (query.PageIndex > -1) //分页添加行号
            {
                ctx.Append("Row_Number() Over (Order By ");
                for (int i = 0; i < query.SortItems.Count; i++)
                {
                    SqlSortItem si = query.SortItems[i];
                    BuildExpression(si.Expression, ctx);
                    if (si.SortType == SortType.DESC)
                        ctx.Append(" DESC");
                    if (i < query.SortItems.Count - 1)
                        ctx.Append(" ,");
                }
                ctx.Append(") _rn");
            }
            else
                ctx.RemoveLastChar(); //移除最后多余的,号

            //构建From
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildFrom;
            ctx.Append(" From ");
            //判断From源
            if (query.IsOutQuery)
            {
                SqlFromQuery q = (SqlFromQuery)ctx.CurrentQuery;
                //开始构建From子查询
                ctx.Append("(");
                BuildQuery(q.Target, ctx);
                ctx.Append(")");
                ctx.AppendFormat(" {0}", ((SqlQueryBase)q.Target).AliasName);// ((QueryBase)query).AliasName);
            }
            else
            {
                SqlQuery q = (SqlQuery)ctx.CurrentQuery;
                var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(q.T.ModelID).Result;
                ctx.AppendFormat("\"{0}\" {1}", model.Name, q.AliasName);
            }

            //构建Where
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildWhere;
            if (!Equals(null, ctx.CurrentQuery.Filter))
            {
                ctx.Append(" Where ");
                BuildExpression(ctx.CurrentQuery.Filter, ctx);
            }

            //在不分页的情况下构建Order By 
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildOrderBy;
            if (query.PageIndex == -1 && query.HasSortItems)
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

            //构建Join
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildJoin;
            SqlQueryBase q1 = (SqlQueryBase)ctx.CurrentQuery;
            if (q1.HasJoins) //先处理每个手工的联接及每个手工联接相应的自动联接
            {
                BuildJoins(q1.Joins, ctx);
            }
            //TODO:*****
            //var autoJoins = ctx.GetQueryAutoJoins(q1); //再处理自动联接
            //for (int i = 0; i < autoJoins.Length; i++)
            //{
            //    var rq = autoJoins[i];
            //    var rqModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.ModelID).Result;
            //    var rqOwnerModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.Owner.ModelID).Result;
            //    ctx.AppendFormat(" Left Join \"{0}\" {1} On {1}.\"ID\"={2}.\"{3}\"",
            //        rqModel.Name, rq.AliasName, rq.Owner.AliasName, ((EntityRefModel)rqOwnerModel[rq.Name]).IDMemberName);
            //}

            //处理分页或Top
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildPageTail;
            if (query.PageIndex > -1)
            {
                ctx.Append(") Select *,(Select max(_rn) From tt) _tr From tt Where _rn Between @");
                ctx.Append(ctx.GetParameterName(query.PageIndex * query.TopOrPageSize + 1));
                ctx.Append(" And @");
                ctx.Append(ctx.GetParameterName((query.PageIndex + 1) * query.TopOrPageSize));
            }
            else //不分页追加top等价的limit
            {
                if (query.Purpose == QueryPurpose.ToSingleEntity)
                    ctx.Append(" Limit 1 ");
                else if (query.TopOrPageSize > 0)
                    ctx.AppendFormat(" Limit {0} ", query.TopOrPageSize);
            }

            //结束上下文
            ctx.EndBuildQuery(query);
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
            throw new NotImplementedException();
            //foreach (var item in joins)
            //{
            //    //先处理当前的联接
            //    ctx.Append(GetJoinString(item.JoinType));
            //    if (item.Right is SqlQueryJoin)
            //    {
            //        SqlQueryJoin j = (SqlQueryJoin)item.Right;
            //        var jModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(j.T.ModelID).Result;
            //        ctx.AppendFormat("\"{0}\" {1} On ", jModel.Name, j.AliasName);
            //        BuildExpression(item.OnConditon, ctx);

            //        //再处理手工联接的自动联接
            //        EntityExpression[] autoJoins = ctx.GetQueryAutoJoins(j);
            //        for (int i = 0; i < autoJoins.Length; i++)
            //        {
            //            var rq = autoJoins[i];
            //            var rqModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.ModelID).Result;
            //            var rqOwnerModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(rq.Owner.ModelID).Result;
            //            ctx.AppendFormat(" Left Join \"{0}\" {1} On {1}.\"ID\"={2}.\"{3}\"",
            //                rqModel.Name, rq.AliasName, rq.Owner.AliasName, ((EntityRefModel)rqOwnerModel[rq.Name]).IDMemberName);
            //        }
            //    }
            //    else //否则表示联接对象是SubQuery，注意：子查询没有自动联接
            //    {
            //        SqlSubQuery sq = (SqlSubQuery)item.Right;
            //        ctx.Append("(");
            //        BuildQuery(sq.Target, ctx);
            //        ctx.AppendFormat(") As {0} On ", ((SqlQueryBase)sq.Target).AliasName);
            //        BuildExpression(item.OnConditon, ctx);
            //    }

            //    //最后递归当前联接的右部是否还有手工的联接项
            //    if (item.Right.HasJoins)
            //        BuildJoins(item.Right.Joins, ctx);
            //}
        }

        private string GetJoinString(JoinType joinType)
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
                //case ExpressionType.GroupExpression:
                //    BuildGroupExpression((GroupExpression)exp, ctx);
                //    break;
                case ExpressionType.SelectItemExpression:
                    BuildSelectItem((SqlSelectItemExpression)exp, ctx);
                    break;
                //case ExpressionType.SubQueryExpression:
                //    BuildSubQuery((SubQuery)exp, ctx);
                //    break;
                ////case ExpressionType.DbParameterExpression:
                ////    BuildDbParameterExpression((DbParameterExpression)exp, ctx);
                ////    break;
                //case ExpressionType.InvocationExpression:
                //    BuildInvocationExpression((InvocationExpression)exp, ctx);
                //    break;
                default:
                    throw new NotSupportedException($"Not Supported Expression Type [{exp.Type.ToString()}] for Query.");
            }
        }

        private void BuildPrimitiveExpression(PrimitiveExpression exp, BuildQueryContext ctx)
        {
            if (exp.Value == null)
            {
                ctx.Append("NULL");
                return;
            }

            ctx.AppendFormat("@{0}", ctx.GetParameterName(exp.Value));
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
                    throw new System.Exception("BuildBinaryExpression Error.");
            }
            else
            {
                //操作符
                BuildBinaryOperatorType(exp, ctx.CurrentQueryInfo.Out);
                //右操作数
                BuildExpression(exp.RightOperand, ctx);
            }
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
                //case BinaryOperatorType.Plus:
                //    if (CheckNeedConvertStringAddOperator(exp))
                //        sb.Append(" || ");
                //    else
                //        sb.Append(" + ");
                //    break;
                default:
                    throw new NotSupportedException();
            }
        }
        #endregion
    }
}
