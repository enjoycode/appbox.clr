using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Npgsql;
using appbox.Caching;
using appbox.Models;
using appbox.Data;
using System.Diagnostics;

namespace appbox.Store
{
    partial class PgSqlStore
    {
        protected override IList<DbCommand> MakeCreateTable(EntityModel model, Server.IDesignContext ctx)
        {
            //List<DbCommand> funcCmds = new List<DbCommand>();
            List<string> fks = new List<string>(); //引用外键集合

            var sb = StringBuilderCache.Acquire();
            //Build Create Table
            sb.Append($"CREATE TABLE \"{model.SqlTableName}\" (");
            foreach (var mm in model.Members)
            {
                if (mm.Type == EntityMemberType.DataField)
                {
                    var dfm = (DataFieldModel)mm;
                    BuildFieldDefine(dfm.SqlColName, dfm.DataType, 0 /*fix length*/, 0/*fix scale*/, dfm.AllowNull, sb, false);
                    sb.Append(',');
                }
                else if (mm.Type == EntityMemberType.EntityRef)
                {
                    var rm = (EntityRefModel)mm;
                    if (!rm.IsAggregationRef) //只有非聚合引合创建外键
                    {
                        fks.Add(BuildForeignKey(rm, ctx));
                        //考虑旧实现CreateGetTreeNodeChildsDbFuncCommand
                    }
                }
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(");");

            //Build PrimaryKey
            if (model.SqlStoreOptions.HasPrimaryKeys)
            {
                sb.AppendLine();
                sb.Append($"ALTER TABLE \"{model.Name}\" ADD CONSTRAINT \"PK_{model.Id}\"");
                sb.Append(" PRIMARY KEY (");
                foreach (var pk in model.SqlStoreOptions.PrimaryKeys)
                {
                    var mm = model.GetMember(pk.MemberId, true);
                    sb.Append($"\"{mm.Name}\",");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(");");
            }
            //加入EntityRef引用外键
            sb.AppendLine();
            for (int i = 0; i < fks.Count; i++)
            {
                sb.AppendLine(fks[i]);
            }

            var res = new List<DbCommand>();
            res.Add(new NpgsqlCommand(StringBuilderCache.GetStringAndRelease(sb)));

            //Build Indexes
            BuildIndexes(model, res);

            return res;
        }

        protected override IList<DbCommand> MakeAlterTable(EntityModel model, Server.IDesignContext ctx)
        {
            //TODO:***处理主键变更
            StringBuilder sb;
            bool needCommand = false; //用于判断是否需要处理NpgsqlCommand
            List<string> fks = new List<string>(); //引用外键列表
            List<DbCommand> commands = new List<DbCommand>();
            //List<DbCommand> funcCmds = new List<DbCommand>();
            //先处理表名称有没有变更，后续全部使用新名称
            if (model.IsNameChanged)
            {
                var renameTableCmd = new NpgsqlCommand($"ALTER TABLE \"{model.SqlTableOriginalName}\" RENAME TO \"{model.SqlTableName}\"");
                commands.Add(renameTableCmd);
            }

            //处理删除的成员
            var deletedMembers = model.Members.Where(t => t.PersistentState == PersistentState.Deleted).ToArray();
            if (deletedMembers != null && deletedMembers.Length > 0)
            {
                #region ----删除的成员----
                sb = StringBuilderCache.Acquire();
                foreach (var m in deletedMembers)
                {
                    if (m.Type == EntityMemberType.DataField)
                    {
                        needCommand = true;
                        sb.AppendFormat("ALTER TABLE \"{0}\" DROP COLUMN \"{1}\";",
                            model.SqlTableName, ((DataFieldModel)m).SqlColOriginalName);
                    }
                    else if (m.Type == EntityMemberType.EntityRef)
                    {
                        EntityRefModel rm = (EntityRefModel)m;
                        if (!rm.IsAggregationRef)
                        {
                            var fkName = $"FK_{rm.MemberId}"; //TODO:特殊处理DbFirst导入表的外键约束名称
                            fks.Add($"ALTER TABLE \"{model.SqlTableName}\" DROP CONSTRAINT \"{fkName}\";");
                        }
                    }
                }

                var cmdText = StringBuilderCache.GetStringAndRelease(sb);
                if (needCommand)
                {
                    //加入删除的外键SQL
                    for (int i = 0; i < fks.Count; i++)
                    {
                        sb.Insert(0, fks[i]);
                        sb.AppendLine();
                    }

                    commands.Add(new NpgsqlCommand(cmdText));
                }
                #endregion
            }

            //reset
            needCommand = false;
            fks.Clear();

            //处理新增的成员
            var addedMembers = model.Members.Where(t => t.PersistentState == PersistentState.Detached).ToArray();
            if (addedMembers != null && addedMembers.Length > 0)
            {
                #region ----新增的成员----
                sb = StringBuilderCache.Acquire();
                foreach (var m in addedMembers)
                {
                    if (m.Type == EntityMemberType.DataField)
                    {
                        needCommand = true;
                        sb.AppendFormat("ALTER TABLE \"{0}\" ADD COLUMN ", model.SqlTableName);
                        DataFieldModel dfm = (DataFieldModel)m;
                        BuildFieldDefine(dfm.Name, dfm.DataType, 0 /*fix length*/, 0/*fix scale*/, dfm.AllowNull, sb, false);
                        sb.Append(";");
                    }
                    else if (m.Type == EntityMemberType.EntityRef)
                    {
                        var rm = (EntityRefModel)m;
                        if (!rm.IsAggregationRef) //只有非聚合引合创建外键
                        {
                            fks.Add(BuildForeignKey(rm, ctx));
                            //考虑CreateGetTreeNodeChildsDbFuncCommand
                        }
                    }
                }

                var cmdText = StringBuilderCache.GetStringAndRelease(sb);
                if (needCommand)
                {
                    //加入关系
                    sb.AppendLine();
                    for (int i = 0; i < fks.Count; i++)
                    {
                        sb.AppendLine(fks[i]);
                    }

                    commands.Add(new NpgsqlCommand(cmdText));
                }
                #endregion
            }

            //reset
            needCommand = false;
            fks.Clear();

            //处理修改的成员
            var changedMembers = model.Members.Where(t => t.PersistentState == PersistentState.Modified).ToArray();
            if (changedMembers != null && changedMembers.Length > 0)
            {
                #region ----修改的成员----
                foreach (var m in changedMembers)
                {
                    if (m.Type == EntityMemberType.DataField)
                    {
                        DataFieldModel dfm = (DataFieldModel)m;
                        //TODO: 先处理数据类型变更 ALTER TABLE products ALTER COLUMN price TYPE numeric(10,2);

                        //再处理重命名列
                        if (m.IsNameChanged)
                        {
                            var renameColCmd = new NpgsqlCommand($"ALTER TABLE \"{model.SqlTableName}\" RENAME COLUMN \"{dfm.SqlColOriginalName}\" TO \"{dfm.SqlColName}\"");
                            commands.Add(renameColCmd);
                        }
                    }

                    //TODO:处理EntityRef更新与删除规则
                    //注意不再需要同旧实现一样变更EntityRef的外键约束名称 "ALTER TABLE \"XXX\" RENAME CONSTRAINT \"XXX\" TO \"XXX\""
                    //因为ModelFirst的外键名称为FK_{MemberId}；CodeFirst为导入的名称
                }
                #endregion
            }

            //处理索引变更
            BuildIndexes(model, commands);

            return commands;
        }

        protected override DbCommand MakeDropTable(EntityModel model)
        {
            return new NpgsqlCommand($"DROP TABLE IF EXISTS \"{model.SqlTableOriginalName}\"");
        }

        #region ====Help Methods====
        private static string GetActionRuleString(EntityRefActionRule rule)
        {
            return rule switch
            {
                EntityRefActionRule.NoAction => "NO ACTION",
                EntityRefActionRule.Cascade => "CASCADE",
                EntityRefActionRule.SetNull => "SET NULL",
                _ => "NO ACTION",
            };
        }

        private static string BuildFieldDefine(string fieldName, EntityFieldType dataType, int length,
            int scale, bool allowNull, StringBuilder sb, bool isAlterFieldType)
        {
            string defaultValue = string.Empty;
            sb.Append($"\"{fieldName}\" ");
            if (isAlterFieldType)
                sb.Append("TYPE ");

            switch (dataType)
            {
                case EntityFieldType.String:
                    defaultValue = "''";
                    if (length == 0)
                        sb.Append("text ");
                    else
                        sb.Append($"varchar({length}) ");
                    break;
                case EntityFieldType.DateTime:
                    defaultValue = "'0001-1-1'"; //TODO: fix it
                    sb.Append("timestamp ");
                    break;
                case EntityFieldType.UInt16:
                case EntityFieldType.Int16:
                    defaultValue = "0";
                    sb.Append("int2 ");
                    break;
                case EntityFieldType.UInt32:
                case EntityFieldType.Int32:
                    defaultValue = "0";
                    sb.Append("int4 ");
                    break;
                case EntityFieldType.UInt64:
                case EntityFieldType.Int64:
                    defaultValue = "0";
                    sb.Append("int8 ");
                    break;
                case EntityFieldType.Decimal:
                    defaultValue = "0";
                    int l = length + scale;
                    sb.AppendFormat("decimal({0},{1}) ", l, scale);
                    break;
                case EntityFieldType.Boolean:
                    defaultValue = "false";
                    sb.Append("bool ");
                    break;
                case EntityFieldType.Guid:
                    defaultValue = "'00000000-0000-0000-0000-000000000000'";
                    sb.Append("uuid ");
                    break;
                case EntityFieldType.Byte:
                    defaultValue = "0";
                    sb.Append("int2 ");
                    break;
                case EntityFieldType.Enum:
                    defaultValue = "0";
                    sb.Append("int4 ");
                    break;
                case EntityFieldType.Float:
                    defaultValue = "0";
                    sb.Append("float4 ");
                    break;
                case EntityFieldType.Double:
                    defaultValue = "0";
                    sb.Append("float8 ");
                    break;
                case EntityFieldType.Binary:
                    sb.Append("bytea ");
                    break;
                default:
                    throw new NotImplementedException("PgSqlStore.BuildFieldDefine");
            }

            if (!allowNull && !isAlterFieldType)
            {
                if (dataType == EntityFieldType.Binary)
                    throw new Exception("Binary field must be allow null");

                sb.Append("NOT NULL DEFAULT ");
                sb.Append(defaultValue);
            }

            return defaultValue;
        }

        private static string BuildForeignKey(EntityRefModel rm, Server.IDesignContext ctx)
        {
            var refModel = ctx.GetEntityModel(rm.RefModelIds[0]);
            //使用成员标识作为fk name以减少重命名带来的影响
            var fkName = $"FK_{rm.MemberId}";
            var rsb = StringBuilderCache.Acquire();
            rsb.Append($"ALTER TABLE \"{rm.Owner.SqlTableName}\" ADD CONSTRAINT \"{fkName}\" FOREIGN KEY (");
            for (int i = 0; i < rm.FKMemberIds.Length; i++)
            {
                var fk = (DataFieldModel)rm.Owner.GetMember(rm.FKMemberIds[i], true);
                if (i != 0) rsb.Append(',');
                rsb.Append($"\"{fk.SqlColName}\"");
            }
            rsb.Append($") REFERENCES \"{refModel.SqlTableName}\" (");
            for (int i = 0; i < refModel.SqlStoreOptions.PrimaryKeys.Count; i++)
            {
                var pk = (DataFieldModel)refModel.GetMember(refModel.SqlStoreOptions.PrimaryKeys[i].MemberId, true);
                if (i != 0) rsb.Append(',');
                rsb.Append($"\"{pk.SqlColName}\"");
            }
            //TODO:pg's MATCH SIMPLE?
            rsb.Append($") ON UPDATE {GetActionRuleString(rm.UpdateRule)} ON DELETE {GetActionRuleString(rm.DeleteRule)};");
            return StringBuilderCache.GetStringAndRelease(rsb);
        }

        private static void BuildIndexes(EntityModel model, List<DbCommand> commands)
        {
            Debug.Assert(commands != null);
            if (!model.SqlStoreOptions.HasIndexes)
                return;

            if (model.PersistentState != PersistentState.Detached)
            {
                var deletedIndexes = model.SqlStoreOptions.Indexes.Where(t => t.PersistentState == PersistentState.Deleted);
                foreach (var index in deletedIndexes)
                {
                    commands.Add(new NpgsqlCommand($"DROP INDEX IF EXISTS \"IX_{index.IndexId}\""));
                }
            }

            var newIndexes = model.SqlStoreOptions.Indexes.Where(t => t.PersistentState == PersistentState.Detached);
            foreach (var index in newIndexes)
            {
                var sb = StringBuilderCache.Acquire();
                sb.Append("CREATE ");
                if (index.Unique) sb.Append("UNIQUE ");
                sb.Append($"INDEX \"IX_{index.IndexId}\" ON \"{model.SqlTableName}\" (");
                for (int i = 0; i < index.Fields.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    var dfm = (DataFieldModel)model.GetMember(index.Fields[i].MemberId, true);
                    sb.Append($"\"{dfm.SqlColName}\"");
                    if (index.Fields[i].OrderByDesc) sb.Append(" DESC");
                }
                sb.Append(')');
                commands.Add(new NpgsqlCommand(StringBuilderCache.GetStringAndRelease(sb)));
            }

            //暂不处理改变的索引
        }
        #endregion
    }
}
