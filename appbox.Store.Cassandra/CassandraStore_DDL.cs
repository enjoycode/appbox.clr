using System;
using System.Linq;
using System.Text;
using appbox.Caching;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    partial class CassandraStore
    {
        private static void BuildFieldDefinition(StringBuilder sb, EntityMemberModel member)
        {
            switch (member.Type)
            {
                case EntityMemberType.DataField:
                    //case EntityMemberType.FieldSet: //TODO:fix FieldSet
                    {
                        var fieldName = member.Name;
                        //var dataType = member.Type == EntityMemberType.DataField ?
                        //    ((DataFieldModel)member).DataType : ((FieldSetModel)member).DataType;
                        var dataType = ((DataFieldModel)member).DataType;
                        sb.Append($"\"{fieldName}\" ");
                        //if (member.Type == EntityMemberType.FieldSet)
                        //    sb.Append(" set<");
                        switch (dataType)
                        {
                            case EntityFieldType.String:
                                sb.Append("text"); break;
                            case EntityFieldType.Byte:
                                sb.Append("tinyint"); break;
                            case EntityFieldType.Int16:
                                sb.Append("smallint"); break;
                            case EntityFieldType.Enum:
                            case EntityFieldType.Int32:
                                sb.Append("int"); break;
                            case EntityFieldType.Int64:
                                sb.Append("bigint"); break;
                            case EntityFieldType.DateTime:
                                sb.Append("timestamp"); break;
                            case EntityFieldType.Guid:
                                sb.Append("uuid"); break;
                            case EntityFieldType.Boolean:
                                sb.Append("boolean"); break;
                            case EntityFieldType.Float:
                                sb.Append("float"); break;
                            case EntityFieldType.Double:
                                sb.Append("double"); break;
                            case EntityFieldType.Binary:
                                sb.Append("blob"); break;
                            default:
                                throw new NotImplementedException();
                        }
                        //if (member.Type == EntityMemberType.FieldSet)
                        //    sb.Append(">");
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void BuildPrimaryKey(StringBuilder sb, CqlPrimaryKey pkey, EntityModel model)
        {
            sb.Append(" PRIMARY KEY (");
            var partitionKeys = pkey.PartitionKeys;
            if (partitionKeys != null && partitionKeys.Length > 0)
            {
                sb.Append("(");
                for (int i = 0; i < partitionKeys.Length; i++)
                {
                    if (i != 0)
                        sb.Append(",");
                    sb.Append($"\"{model.GetMember(partitionKeys[i], true).Name}\"");
                }
                sb.Append(")");
            }
            var clusteringCols = pkey.ClusteringColumns;
            if (clusteringCols != null && clusteringCols.Length > 0)
            {
                for (int i = 0; i < clusteringCols.Length; i++)
                {
                    sb.Append($",\"{model.GetMember(clusteringCols[i].MemberId, true).Name}\"");
                }
            }
            sb.Append(")");
        }

        private static bool BuildOrderBy(StringBuilder sb, CqlPrimaryKey pkey, EntityModel model)
        {
            //检查是否需要
            bool needBuild = false;
            if (pkey.ClusteringColumns != null)
            {
                for (int i = 0; i < pkey.ClusteringColumns.Length; i++)
                {
                    if (pkey.ClusteringColumns[i].OrderByDesc)
                    {
                        needBuild = true; break;
                    }
                }
            }
            if (needBuild)
            {
                sb.Append(" WITH CLUSTERING ORDER BY (");
                for (int i = 0; i < pkey.ClusteringColumns.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append($"\"{model.GetMember(pkey.ClusteringColumns[i].MemberId, true).Name}\" ");
                    if (pkey.ClusteringColumns[i].OrderByDesc)
                        sb.Append("DESC");
                    else
                        sb.Append("ASC");
                }
                sb.Append(')');
            }

            return needBuild;
        }

        /// <summary>
        /// 处理新建或修改时创建或删除物化视图
        /// </summary>
        private void ProcessMaterializedViews(EntityModel model)
        {
            if (!model.CqlStoreOptions.HasMaterializedView)
                return;

            if (model.PersistentState != PersistentState.Detached)
            {
                var deletedViews = model.CqlStoreOptions.MaterializedViews.Where(t => t.PersistentState == PersistentState.Deleted);
                foreach (var view in deletedViews)
                {
                    DropMV(model, view);
                }
            }

            var newViews = model.CqlStoreOptions.MaterializedViews.Where(t => t.PersistentState == PersistentState.Detached);
            foreach (var view in newViews)
            {
                CreateMV(model, view);
            }
        }

        /// <summary>
        /// 新建物化视图
        /// </summary>
        private void CreateMV(EntityModel model, CqlMaterializedView view)
        {
            //TODO:考虑先尝试移除已存在的
            var sb = StringBuilderCache.Acquire();
            sb.Append("CREATE MATERIALIZED VIEW ");
            sb.Append($"\"{model.Id}_{view.Name}\" AS ");
            sb.Append($"SELECT * FROM \"{model.Name}\" WHERE "); //TODO:暂Select *
            for (int i = 0; i < view.PrimaryKey.PartitionKeys.Length; i++)
            {
                if (i != 0) sb.Append(" And ");
                sb.Append($"\"{view.PrimaryKey.PartitionKeys[i]}\" IS NOT NULL");
            }
            if (view.PrimaryKey.ClusteringColumns != null)
            {
                for (int i = 0; i < view.PrimaryKey.ClusteringColumns.Length; i++)
                {
                    sb.Append($" And \"{model.GetMember(view.PrimaryKey.ClusteringColumns[i].MemberId, true).Name}\" IS NOT NULL");
                }
            }

            BuildPrimaryKey(sb, view.PrimaryKey, model);
            BuildOrderBy(sb, view.PrimaryKey, model);

            session.Execute(StringBuilderCache.GetStringAndRelease(sb));
        }

        /// <summary>
        /// 删除物化视图
        /// </summary>
        private void DropMV(EntityModel model, CqlMaterializedView view)
        {
            session.Execute($"DROP MATERIALIZED VIEW IF EXISTS \"{model.Id}_{view.Name}\"");
        }
    }
}
