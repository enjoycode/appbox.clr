using System;
using System.Data.Common;
using appbox.Models;
using appbox.Expressions;
using Npgsql;

namespace appbox.Store
{
    partial class PgSqlStore
    {
        protected override DbCommand BuidUpdateCommand(SqlUpdateCommand updateCommand)
        {
            NpgsqlCommand cmd = new NpgsqlCommand();
            BuildQueryContext ctx = new BuildQueryContext(cmd, updateCommand);
            //设置上下文
            ctx.BeginBuildQuery(updateCommand);

            EntityModel model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(updateCommand.T.ModelID).Result;

            ctx.AppendFormat("Update \"{0}\" t Set ", model.Name);
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildUpdateSet;
            for (int i = 0; i < updateCommand.UpdateItems.Count; i++)
            {
                BuildExpression(updateCommand.UpdateItems[i], ctx);
                if (i < updateCommand.UpdateItems.Count - 1)
                    ctx.Append(",");
            }

            //构建Where
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildWhere;
            if (!Expression.IsNull(updateCommand.Filter))
            {
                ctx.Append(" Where ");
                BuildExpression(ctx.CurrentQuery.Filter, ctx);
            }

            //构建Join
            ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildJoin;
            SqlQueryBase q1 = (SqlQueryBase)ctx.CurrentQuery;
            if (q1.HasJoins) //先处理每个手工的联接及每个手工联接相应的自动联接
            {
                BuildJoins(q1.Joins, ctx);
            }
            ctx.BuildQueryAutoJoins(q1); //再处理自动联接

            //最后处理返回值
            if (updateCommand.HasOutputItems)
            {
                ctx.CurrentQueryInfo.BuildStep = BuildQueryStep.BuildWhere; //TODO: fix this?
                ctx.Append(" RETURNING ");
                for (int i = 0; i < updateCommand.OutputItems.Count; i++)
                {
                    var field = (FieldExpression)updateCommand.OutputItems[i];
                    ctx.AppendFormat("\"{0}\"", field.Name);
                    if (i != updateCommand.OutputItems.Count - 1)
                        ctx.Append(",");
                }
            }

            //结束用于附加条件，注意：仅在Upsert时这样操作
            ctx.EndBuildQuery(updateCommand);
            return cmd;
        }
    }
}
