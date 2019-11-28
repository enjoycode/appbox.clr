using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Npgsql;
using appbox.Caching;
using appbox.Models;

namespace appbox.Store
{
    partial class PgSqlStore
    {
        protected override IList<DbCommand> MakeCreateTable(EntityModel model)
        {
            //List<DbCommand> funcCmds = new List<DbCommand>();
            ////加入引用关系
            //List<string> refRelations = new List<string>(); //引用关系

            var sb = StringBuilderCache.Acquire();
            //Build Create Table
            sb.Append($"CREATE TABLE {FieldEscaper}{model.Name}{FieldEscaper} (");
            foreach (var mm in model.Members)
            {
                if (mm.Type == EntityMemberType.DataField)
                {
                    var dfm = (DataFieldModel)mm;
                    BuildFieldDefine(dfm.Name, false /*fix isrefKey*/,
                        dfm.DataType, 0 /*fix length*/, 0/*fix scale*/, dfm.AllowNull, sb, false);
                    sb.Append(',');
                }
                else if (mm.Type == EntityMemberType.EntityRef)
                {
                    throw new NotImplementedException();
                }
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(");");

            //Build PrimaryKey
            if (model.SqlStoreOptions.HasPrimaryKeys)
            {
                sb.AppendLine();
                sb.Append($"ALTER TABLE {FieldEscaper}{model.Name}{FieldEscaper} ADD CONSTRAINT {FieldEscaper}PK_{model.Id}{FieldEscaper}");
                sb.Append(" PRIMARY KEY (");
                foreach (var pk in model.SqlStoreOptions.PrimaryKeys)
                {
                    var mm = model.GetMember(pk.MemberId, true);
                    sb.Append($"{FieldEscaper}{mm.Name}{FieldEscaper},");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(");");
            }

            //TODO: Build EntityRef's外键
            //TODO: Build Indexes

            var res = new List<DbCommand>();
            res.Add(new NpgsqlCommand(StringBuilderCache.GetStringAndRelease(sb)));

            return res;
        }

        protected override IList<DbCommand> MakeAlterTable(EntityModel model)
        {
            throw new NotImplementedException();
        }

        protected override DbCommand MakeDropTable(EntityModel model)
        {
            throw new NotImplementedException();
        }

        #region ====Help Methods====
        private string BuildFieldDefine(string fieldName, bool isRefKey, EntityFieldType dataType, int length,
            int scale, bool allowNull, StringBuilder sb, bool isAlterFieldType)
        {
            string defaultValue = string.Empty;
            sb.Append($"{FieldEscaper}{fieldName}{FieldEscaper} ");
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
        #endregion
    }
}
