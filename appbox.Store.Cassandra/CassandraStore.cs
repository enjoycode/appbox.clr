﻿using System;
using System.Threading.Tasks;
using Cassandra;
using appbox.Models;
using appbox.Caching;
using System.Linq;
using appbox.Data;

namespace appbox.Store
{
    public sealed partial class CassandraStore : CqlStore
    {
        private readonly Cluster cluster;
        private readonly ISession session;

        #region ====Ctor====
        public CassandraStore(string settings)
        {
            var s = System.Text.Json.JsonSerializer.Deserialize<Settings>(settings);
            cluster = Cluster.Builder().AddContactPoints(s.Seeds.Split(',')).Build();
            session = cluster.Connect(s.Keyspace);
        }
        #endregion

        #region ====DML & Execute Methods====
        public override async Task InsertAsync(Entity entity, bool ifNotExists = false)
        {
            var cmd = new SimpleStatement(CqlCommandBuilder.BuildInsertEntityCommand(entity, ifNotExists));
            await session.ExecuteAsync(cmd);
        }

        public override async Task UpdateAsync(Entity entity, bool ifNotExists = false)
        {
            var cmd = new SimpleStatement(CqlCommandBuilder.BuildUpdateEntityCommand(entity, ifNotExists));
            await session.ExecuteAsync(cmd);
        }

        public override async Task DeleteAsync(Entity entity, bool ifNotExists = false)
        {
            var cmd = new SimpleStatement(CqlCommandBuilder.BuildDeleteEntityCommand(entity, ifNotExists));
            await session.ExecuteAsync(cmd);
        }

        public override async Task<IRowSet> ExecuteAsync(string cql)
        {
            var cmd = new SimpleStatement(cql);
            var rawRS = await session.ExecuteAsync(cmd);
            return new RowSet(rawRS);
        }

        public override Task<IRowSet> ExecuteAsync(ref CqlBatch batch)
        {
            if (batch.Commands == null || batch.Commands.Count <= 0)
                throw new ArgumentException("CqlBatch has none commands");

            BatchStatement bst = new BatchStatement();
            ICqlCommand cmd = null;
            for (int i = 0; i < batch.Commands.Count; i++)
            {
                cmd = batch.Commands[i];
                if (cmd is CqlCommand)
                {
                    var ecmd = (CqlCommand)cmd;
                    switch (ecmd.Type)
                    {
                        case CqlCommandType.Insert:
                            bst.Add(new SimpleStatement(CqlCommandBuilder.BuildInsertEntityCommand(ecmd.Entity, ecmd.CheckExists)));
                            break;
                        case CqlCommandType.Update:
                            bst.Add(new SimpleStatement(CqlCommandBuilder.BuildUpdateEntityCommand(ecmd.Entity, ecmd.CheckExists)));
                            break;
                        case CqlCommandType.Delete:
                            bst.Add(new SimpleStatement(CqlCommandBuilder.BuildDeleteEntityCommand(ecmd.Entity, ecmd.CheckExists)));
                            break;
                        default:
                            throw new NotSupportedException("CassandraTableStore.Execut batch");
                    }
                }
                else
                {
                    throw new NotImplementedException("CassandraTableStore.Execut batch");
                }
            }

            return ExecuteBatchAsync(bst);
        }

        private async Task<IRowSet> ExecuteBatchAsync(BatchStatement bst)
        {
            var rs = await session.ExecuteAsync(bst);
            return new RowSet(rs);
        }
        #endregion

        #region ====DDL Methods====
        public override async Task CreateTableAsync(EntityModel model)
        {
            //TODO:考虑先尝试移除已存在的
            var sb = StringBuilderCache.Acquire(); ;
            sb.Append($"CREATE TABLE \"{model.Name}\" (");
            //成员
            var members = model.Members;
            for (int i = 0; i < members.Count; i++)
            {
                BuildFieldDefinition(sb, members[i]);
                sb.Append(",");
            }
            //主键
            BuildPrimaryKey(sb, model.CqlStoreOptions.PrimaryKey, model);
            sb.Append(")");
            //OrderBy
            BuildOrderBy(sb, model.CqlStoreOptions.PrimaryKey, model);

            var cmd = new SimpleStatement(StringBuilderCache.GetStringAndRelease(sb));
            await session.ExecuteAsync(cmd);

            //处理相关物化视图
            ProcessMaterializedViews(model);
        }

        public override async Task AlterTableAsync(EntityModel model)
        {
            //处理物化视图
            ProcessMaterializedViews(model);

            //处理删除的列
            var deletedMembers = model.Members.Where(t => t.PersistentState == PersistentState.Deleted).ToArray();
            if (deletedMembers != null && deletedMembers.Length > 0)
            {
                var sb = StringBuilderCache.Acquire();
                sb.Append($"ALTER TABLE \"{model.OriginalName}\" DROP (");
                for (int i = 0; i < deletedMembers.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append($"\"{deletedMembers[i].OriginalName}\"");
                }
                sb.Append(')');

                var cmd = new SimpleStatement(StringBuilderCache.GetStringAndRelease(sb));
                await session.ExecuteAsync(cmd);
            }

            //处理新增的列
            var addedMembers = model.Members.Where(t => t.PersistentState == PersistentState.Detached).ToArray();
            if (addedMembers != null && addedMembers.Length > 0)
            {
                var sb = StringBuilderCache.Acquire();
                sb.Append($"ALTER TABLE \"{model.OriginalName}\" ADD (");
                for (int i = 0; i < addedMembers.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    BuildFieldDefinition(sb, addedMembers[i]);
                }
                sb.Append(')');

                var cmd = new SimpleStatement(StringBuilderCache.GetStringAndRelease(sb));
                await session.ExecuteAsync(cmd);
            }

            //TODO:处理索引及选项变更
        }

        public override async Task DropTableAsync(EntityModel model)
        {
            var cmd = new SimpleStatement($"DROP TABLE IF EXISTS \"{model.Name}\"");
            await session.ExecuteAsync(cmd);
        }
        #endregion
    }

    struct Settings
    {
        public string Seeds { get; set; }
        public string Keyspace { get; set; }
    }
}
