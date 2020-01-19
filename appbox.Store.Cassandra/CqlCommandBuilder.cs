using System;
using System.Collections;
using System.Text;
using appbox.Caching;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    internal static class CqlCommandBuilder
    {
        internal static string BuildInsertEntityCommand(Entity entity, bool checkExists = false)
        {
            //TODO: use Prepared statements and cache it, 另可考虑JSON方式

            var model = entity.Model;
            var sb = StringBuilderCache.Acquire();
            sb.Append($"INSERT INTO \"{model.Name}\" (");
            var vsb = StringBuilderCache.Acquire();
            vsb.Append(") VALUES (");

            for (int i = 0; i < entity.Members.Length; i++)
            {
                ref EntityMember m = ref entity.Members[i];
                if (m.HasValue || m.HasChanged)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                        vsb.Append(',');
                    }

                    switch (m.MemberType)
                    {
                        case EntityMemberType.DataField:
                            {
                                DataFieldModel fm = (DataFieldModel)model.GetMember(m.Id, true);
                                sb.Append($"\"{fm.Name}\"");
                                BuildDataFieldValue(vsb, ref m);
                            }
                            break;
                        //case EntityMemberType.FieldSet:
                        //    {
                        //        FieldSetModel fm = (FieldSetModel)model.GetMember(m.Id, true);
                        //        sb.Append($"\"{fm.Name}\"");
                        //        BuildFieldSetValue(vsb, ref m);
                        //    }
                        //    break;
                        default:
                            throw new NotSupportedException("Not supported member type");
                    }
                }
            }
            sb.Append(StringBuilderCache.GetStringAndRelease(vsb));
            sb.Append(')');

            if (checkExists)
                sb.Append(" IF NOT EXISTS");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static string BuildUpdateEntityCommand(Entity entity, bool checkExists = false)
        {
            var model = entity.Model;
            var pk = entity.Model.CqlStoreOptions.PrimaryKey;

            var sb = StringBuilderCache.Acquire();
            sb.Append("UPDATE \"");
            sb.Append(model.Name);
            sb.Append("\" SET ");

            for (int i = 0; i < entity.Members.Length; i++)
            {
                ref EntityMember m = ref entity.Members[i];
                if (m.HasChanged && !pk.IsPrimaryKey(m.Id))
                {
                    if (i != 0) sb.Append(',');

                    switch (m.MemberType)
                    {
                        case EntityMemberType.DataField:
                            {
                                DataFieldModel fm = (DataFieldModel)model.GetMember(m.Id, true);
                                sb.Append($"\"{fm.Name}\"=");
                                BuildDataFieldValue(sb, ref m);
                            }
                            break;
                        //case EntityMemberType.FieldSet:
                        //    {
                        //        FieldSetModel fm = (FieldSetModel)model.GetMember(m.Id, true);
                        //        sb.Append($"\"{fm.Name}\"=");
                        //        BuildFieldSetValue(sb, ref m);
                        //    }
                        //    break;
                        default:
                            throw new NotSupportedException("Not supported member type");
                    }
                }
            }

            //根据主键生成条件
            BuildWhereWithPK(sb, entity);

            if (checkExists)
                sb.Append(" IF EXISTS");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static string BuildDeleteEntityCommand(Entity entity, bool checkExists = false)
        {
            var model = entity.Model;
            var sb = StringBuilderCache.Acquire();
            sb.Append("DELETE FROM \"");
            sb.Append(model.Name);
            sb.Append("\"");
            //根据主键生成条件
            BuildWhereWithPK(sb, entity);

            if (checkExists)
                sb.Append(" IF EXISTS");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// 用于删除及更新单个实体生成Where条件
        /// </summary>
        private static void BuildWhereWithPK(StringBuilder sb, Entity entity)
        {
            var pk = entity.Model.CqlStoreOptions.PrimaryKey;
            sb.Append(" WHERE ");
            for (int i = 0; i < pk.PartitionKeys.Length; i++)
            {
                if (i != 0) sb.Append(" AND ");
                sb.Append("\"");
                sb.Append(pk.PartitionKeys[i]);
                sb.Append("\"=");
                var member = entity.GetMember(pk.PartitionKeys[i]);
                BuildDataFieldValue(sb, ref member);
            }
            if (pk.ClusteringColumns != null && pk.ClusteringColumns.Length > 0)
            {
                for (int i = 0; i < pk.ClusteringColumns.Length; i++)
                {
                    sb.Append(" AND ");
                    sb.Append("\"");
                    sb.Append(entity.Model.GetMember(pk.ClusteringColumns[i].MemberId, true).Name);
                    sb.Append("\"=");
                    ref EntityMember member = ref entity.GetMember(pk.ClusteringColumns[i].MemberId);
                    BuildDataFieldValue(sb, ref member);
                }
            }
        }

        private static void BuildDataFieldValue(StringBuilder sb, ref EntityMember m)
        {
            if (!m.HasValue)
            {
                sb.Append("null");
                return;
            }

            switch (m.ValueType)
            {
                case EntityFieldType.String:
                    sb.Append($"'{m.ObjectValue}'"); break;
                case EntityFieldType.DateTime:
                    sb.Append((long)((m.DateTimeValue - new DateTime(1970, 1, 1)).TotalMilliseconds)); break;
                case EntityFieldType.Byte:
                    sb.Append(m.ByteValue); break;
                case EntityFieldType.Int16:
                    sb.Append(m.Int16Value); break;
                case EntityFieldType.Enum:
                case EntityFieldType.Int32:
                    sb.Append(m.Int32Value); break;
                case EntityFieldType.Int64:
                    sb.Append(m.Int64Value); break;
                case EntityFieldType.Guid:
                    sb.Append(m.GuidValue); break;
                case EntityFieldType.Boolean:
                    sb.Append(m.BooleanValue ? "true" : "false"); break;
                case EntityFieldType.Float:
                    sb.Append(m.FloatValue); break;
                case EntityFieldType.Double:
                    sb.Append(m.DoubleValue); break;
                case EntityFieldType.Binary:
                    sb.Append("0x");
                    sb.Append(StringHelper.ToHexString((byte[])m.ObjectValue)); //TODO:如何优化
                    break;
                default:
                    throw new NotImplementedException(m.ValueType.ToString());
            }
        }

        private static void BuildFieldSetValue(StringBuilder vsb, ref EntityMember m)
        {
            if (!m.HasValue)
            {
                vsb.Append("null");
                return;
            }

            var values = m.ObjectValue as IEnumerable;
            vsb.Append('{');
            bool needQuote = m.ValueType == EntityFieldType.String;
            bool isFirst = true;
            foreach (var value in values)
            {
                if (isFirst)
                    isFirst = false;
                else
                    vsb.Append(',');

                if (needQuote)
                    vsb.Append('\'');
                if (m.ValueType == EntityFieldType.DateTime)
                    vsb.Append((long)(((DateTime)value - new DateTime(1970, 1, 1)).TotalMilliseconds));
                else
                    vsb.Append(value);
                if (needQuote)
                    vsb.Append('\'');
            }
            vsb.Append('}');
        }
    }
}
