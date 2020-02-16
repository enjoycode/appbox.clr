using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using appbox.Models;
using appbox.Runtime;
using System.Data.Common;
using appbox.Data;
using System.Threading.Tasks;
using appbox.Caching;
using System.Diagnostics;

namespace appbox.Store
{
    /// <summary>
    /// SqlStore的基类
    /// </summary>
    public abstract class SqlStore
    {
        public const string TotalRowsColumnName = "_tr"; //TODO:remove it
        public const string RowNumberColumnName = "_rn"; //TODO:remove it

        #region ====Statics====
        private static readonly Dictionary<ulong, SqlStore> sqlStores = new Dictionary<ulong, SqlStore>();

#if !FUTURE
        internal static readonly ulong DefaultSqlStoreId = unchecked((ulong)StringHelper.GetHashCode("Default"));

        internal static SqlStore Default { get; private set; }

        internal static void SetDefaultSqlStore(SqlStore defaultSqlStore)
        {
            Debug.Assert(defaultSqlStore != null);
            sqlStores.Add(DefaultSqlStoreId, defaultSqlStore);
            Default = defaultSqlStore;
        }
#endif

        /// <summary>
        /// 获取SqlStore实例，缓存不存在则创建
        /// </summary>
        public static SqlStore Get(ulong storeId)
        {
            if (!sqlStores.TryGetValue(storeId, out SqlStore res))
            {
                lock (sqlStores)
                {
                    if (!sqlStores.TryGetValue(storeId, out res))
                    {
                        //加载存储模型
                        if (!(ModelStore.LoadModelAsync(storeId).Result is DataStoreModel model)
                            || model.Kind != DataStoreKind.Sql)
                            throw new Exception($"Can't get SqlStore[Id={storeId}]");

                        //根据Provider创建实例
                        var ps = model.Provider.Split(';');
                        var asmPath = Path.Combine(RuntimeContext.Current.AppPath, Server.Consts.LibPath, ps[0] + ".dll");
                        try
                        {
                            var asm = Assembly.LoadFile(asmPath);
                            var type = asm.GetType(ps[1]);
                            res = (SqlStore)Activator.CreateInstance(type, model.Settings);
                            sqlStores[storeId] = res;
                            Log.Debug($"Create SqlStore instance: {type}, isNull={res == null}");
                            return res;
                        }
                        catch (Exception ex)
                        {
                            var error = $"Create SqlStore[Provider={model.Provider}] instance error: {ex.Message}";
                            throw new Exception(error);
                        }
                    }
                }
            }
            return res;
        }
        #endregion

        #region ====Properties====
        /// <summary>
        /// 名称转义符，如PG用引号包括字段名称\"xxx\"
        /// </summary>
        public abstract string NameEscaper { get; }

        public abstract string ParameterName { get; }

        /// <summary>
        /// 用于消除差异,eg: PgSqlStore=bytea
        /// </summary>
        public abstract string BlobType { get; }

        /// <summary>
        /// 是否支持原子Upsert
        /// </summary>
        public abstract bool IsAtomicUpsertSupported { get; }

        /// <summary>
        /// 某些数据不支持Retuning，所以需要单独读取
        /// </summary>
        public abstract bool UseReaderForOutput { get; }
        #endregion

        #region ====abstract Create Methods====
        public abstract DbConnection MakeConnection();

        public abstract DbCommand MakeCommand();

        public abstract DbParameter MakeParameter();
        #endregion

        #region ====DDL Methods====
        public async Task CreateTableAsync(EntityModel model, DbTransaction txn, Design.IDesignContext ctx)
        {
            Debug.Assert(txn != null);
            var cmds = MakeCreateTable(model, ctx);
            foreach (var cmd in cmds)
            {
                cmd.Connection = txn.Connection;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task AlterTableAsync(EntityModel model, DbTransaction txn, Design.IDesignContext ctx)
        {
            Debug.Assert(txn != null);
            var cmds = MakeAlterTable(model, ctx);
            foreach (var cmd in cmds)
            {
                cmd.Connection = txn.Connection;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DropTableAsync(EntityModel model, DbTransaction txn, Design.IDesignContext ctx)
        {
            Debug.Assert(txn != null);
            var cmd = MakeDropTable(model, ctx);
            cmd.Connection = txn.Connection;
            await cmd.ExecuteNonQueryAsync();
        }

        protected internal abstract IList<DbCommand> MakeCreateTable(EntityModel model, Design.IDesignContext ctx);

        protected internal abstract IList<DbCommand> MakeAlterTable(EntityModel model, Design.IDesignContext ctx);

        protected internal abstract DbCommand MakeDropTable(EntityModel model, Design.IDesignContext ctx);
        #endregion

        #region ====DML Methods====
        //TODO:**** cache Load\Insert\Update\Delete command

        /// <summary>
        /// 从存储加载指定主键的单个实体，不存在返回null
        /// </summary>
        public Task<Entity> LoadAsync(ulong modelId, params EntityMember[] pks)
        {
            return LoadAsync(modelId, null, pks);
        }

        /// <summary>
        /// 从存储加载指定主键的单个实体，不存在返回null
        /// </summary>
        public async Task<Entity> LoadAsync(ulong modelId, DbTransaction txn, params EntityMember[] pks)
        {
            if (pks == null || pks.Length == 0) throw new ArgumentNullException(nameof(pks));

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            if (model.SqlStoreOptions == null || !model.SqlStoreOptions.HasPrimaryKeys
                || model.SqlStoreOptions.PrimaryKeys.Count != pks.Length)
                throw new InvalidOperationException("Can't load entity from sqlstore");

            var cmd = BuildLoadCommand(model, pks);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            if (txn == null)
                await cmd.Connection.OpenAsync();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return SqlQuery.FillEntity(model, reader);
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            finally
            {
                if (txn == null) cmd.Connection.Dispose();
            }
        }

        /// <summary>
        /// 目前主要用于前端新建的实体未设置Guid主键的值时自动生成
        /// </summary>
        private static void AutoGenGuidForPK(Entity entity, EntityModel model)
        {
            if (model.SqlStoreOptions.HasPrimaryKeys)
            {
                for (int i = 0; i < model.SqlStoreOptions.PrimaryKeys.Count; i++)
                {
                    ref EntityMember pk = ref entity.GetMember(model.SqlStoreOptions.PrimaryKeys[i].MemberId);
                    if (pk.ValueType == EntityFieldType.Guid && (!pk.Flag.HasValue || pk.GuidValue == Guid.Empty))
                        pk.GuidValue = Guid.NewGuid(); //TODO:考虑顺序Guid
                }
            }
        }

        public async Task<int> InsertAsync(Entity entity, DbTransaction txn = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.PersistentState != PersistentState.Detached)
                throw new InvalidOperationException("Can't insert none new entity");

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(entity.ModelId);
            if (model.SqlStoreOptions == null)
                throw new InvalidOperationException("Can't insert entity to sqlstore");

            //TODO:暂在这里处理前端未设置的Guid主键，另考虑前端实现UUID，则可以移除
            AutoGenGuidForPK(entity, model);

            var cmd = BuildInsertCommand(entity, model);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            cmd.Transaction = txn;
            if (txn == null)
                await cmd.Connection.OpenAsync();
            Log.Debug(cmd.CommandText);
            //执行命令
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            finally
            {
                if (txn == null) cmd.Connection.Dispose();
            }
        }

        /// <summary>
        /// 仅适用于更新具备主键的实体，否则使用SqlUpdateCommand明确字段及条件更新
        /// </summary>
        public async Task<int> UpdateAsync(Entity entity, DbTransaction txn = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.PersistentState == PersistentState.Detached)
                throw new InvalidOperationException("Can't update new entity");
            if (entity.PersistentState == PersistentState.Deleted)
                throw new InvalidOperationException("Entity already deleted");

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(entity.ModelId);
            if (model.SqlStoreOptions == null)
                throw new InvalidOperationException("Can't update entity to sqlstore");
            if (!model.SqlStoreOptions.HasPrimaryKeys)
                throw new InvalidOperationException("Can't update entity without primary key");

            var cmd = BuildUpdateCommand(entity, model);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            cmd.Transaction = txn;
            if (txn == null)
                await cmd.Connection.OpenAsync();
            Log.Debug(cmd.CommandText);
            //执行命令
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            finally
            {
                if (txn == null) cmd.Connection.Dispose();
            }
        }

        /// <summary>
        /// 仅适用于删除具备主键的实体，否则使用SqlDeleteCommand明确指定条件删除
        /// </summary>
        public async Task<int> DeleteAsync(Entity entity, DbTransaction txn = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.PersistentState == PersistentState.Detached)
                throw new InvalidOperationException("Can't delete new entity");
            if (entity.PersistentState == PersistentState.Deleted)
                throw new InvalidOperationException("Entity already deleted");

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(entity.ModelId);
            if (model.SqlStoreOptions == null)
                throw new InvalidOperationException("Can't delete entity from sqlstore");
            if (!model.SqlStoreOptions.HasPrimaryKeys)
                throw new InvalidOperationException("Can't delete entity without primary key");

            var cmd = BuildDeleteCommand(entity, model);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            cmd.Transaction = txn;
            if (txn == null)
                await cmd.Connection.OpenAsync();
            Log.Debug(cmd.CommandText);
            //执行命令
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            finally
            {
                if (txn == null) cmd.Connection.Dispose();
            }
        }

        public async Task ExecCommandAsync(SqlUpdateCommand updateCommand, DbTransaction txn = null)
        {
            //暂不支持无条件更新，以防止误操作
            if (Expressions.Expression.IsNull(updateCommand.Filter))
                throw new NotSupportedException("Update must assign Where condition");

            var cmd = BuidUpdateCommand(updateCommand);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            cmd.Transaction = txn;
            if (txn == null)
                await cmd.Connection.OpenAsync();
            Log.Debug(cmd.CommandText);
            //执行命令
            if (updateCommand.HasOutputItems && UseReaderForOutput) //返回字段通过DbReader读取
            {
                try
                {
                    var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync()) //TODO:*****循环Read多条记录的返回值
                    {
                        for (int i = 0; i < updateCommand.OutputItems.Count; i++)
                        {
                            updateCommand.OutputValues[i] = reader.GetValue(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                    throw;
                }
                finally
                {
                    if (txn == null) cmd.Connection.Dispose();
                }
            }
            else
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                    throw;
                }
                finally
                {
                    if (txn == null) cmd.Connection.Dispose();
                }

                if (updateCommand.HasOutputItems)
                {
                    throw new NotImplementedException(); //TODO:读取输出参数值
                }
            }
        }

        public async Task<int> ExecCommandAsync(SqlDeleteCommand deleteCommand, DbTransaction txn = null)
        {
            //暂不支持无条件删除，以防止误操作
            if (Expressions.Expression.IsNull(deleteCommand.Filter))
                throw new NotSupportedException("Delete must assign Where condition");

            var cmd = BuildDeleteCommand(deleteCommand);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            cmd.Transaction = txn;
            if (txn == null)
                await cmd.Connection.OpenAsync();
            Log.Debug(cmd.CommandText);
            //执行命令
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Exec sql error: {ex.Message}\n{cmd.CommandText}");
                throw;
            }
            finally
            {
                if (txn == null) cmd.Connection.Dispose();
            }
        }

        /// <summary>
        /// 根据主键值生成加载单个实体的sql
        /// </summary>
        protected internal virtual DbCommand BuildLoadCommand(EntityModel model, EntityMember[] pks)
        {
            var cmd = MakeCommand();
            var sb = StringBuilderCache.Acquire();
            int pindex = 0;
            EntityMemberModel mm;
            var tableName = model.GetSqlTableName(false, null);

            sb.Append($"Select * From {NameEscaper}{tableName}{NameEscaper} Where ");
            for (int i = 0; i < model.SqlStoreOptions.PrimaryKeys.Count; i++)
            {
                pindex++;
                var para = MakeParameter();
                para.ParameterName = $"V{pindex}";
                para.Value = pks[i].BoxedValue;
                cmd.Parameters.Add(para);

                //注意隐式转换的pk的MemberId = 0, 所以不要读pks[i].Id
                mm = model.GetMember(model.SqlStoreOptions.PrimaryKeys[i].MemberId, true);
                if (i != 0) sb.Append(" And ");
                sb.Append($"{NameEscaper}{mm.Name}{NameEscaper}=@{para.ParameterName}");
            }
            sb.Append(" Limit 1");

            cmd.CommandText = StringBuilderCache.GetStringAndRelease(sb);
            return cmd;
        }

        /// <summary>
        /// 根据Entity及其模型生成相应的Insert命令
        /// </summary>
        protected internal virtual DbCommand BuildInsertCommand(Entity entity, EntityModel model)
        {
            var cmd = MakeCommand();
            var sb = StringBuilderCache.Acquire();
            var psb = StringBuilderCache.Acquire(); //用于构建参数列表
            int pindex = 0;
            string sep = "";
            EntityMemberModel mm;
            var tableName = model.GetSqlTableName(false, null);
            //开始构建Sql
            sb.Append($"Insert Into {NameEscaper}{tableName}{NameEscaper} (");
            for (int i = 0; i < entity.Members.Length; i++)
            {
                mm = model.GetMember(entity.Members[i].Id, true);
                if (mm.Type == EntityMemberType.DataField && entity.Members[i].Flag.HasValue)
                {
                    var dfm = (DataFieldModel)mm;
                    pindex++;
                    var para = MakeParameter();
                    para.ParameterName = $"V{pindex}";
                    switch (dfm.DataType)
                    {
                        case EntityFieldType.UInt16:
                            para.Value = unchecked((short)entity.Members[i].UInt16Value);
                            break;
                        case EntityFieldType.UInt32:
                            para.Value = unchecked((int)entity.Members[i].UInt32Value);
                            break;
                        case EntityFieldType.UInt64:
                            para.Value = unchecked((long)entity.Members[i].UInt64Value);
                            break;
                        default:
                            para.Value = entity.Members[i].BoxedValue; //TODO: no boxing
                            break;
                    }
                    cmd.Parameters.Add(para);

                    sb.Append($"{sep}{NameEscaper}{dfm.SqlColName}{NameEscaper}");
                    psb.Append($"{sep}@{para.ParameterName}");

                    if (pindex == 1) sep = ",";
                }

            }
            sb.Append(") Values (");
            sb.Append(StringBuilderCache.GetStringAndRelease(psb));
            sb.Append(")");

            cmd.CommandText = StringBuilderCache.GetStringAndRelease(sb);
            return cmd;
        }

        protected internal virtual DbCommand BuildDeleteCommand(Entity entity, EntityModel model)
        {
            var cmd = MakeCommand();
            var sb = StringBuilderCache.Acquire();
            var tableName = model.GetSqlTableName(false, null);

            sb.Append($"Delete From {NameEscaper}{tableName}{NameEscaper} Where ");
            //根据主键生成条件
            BuildWhereForUpdateOrDelete(entity, model, cmd, sb);

            cmd.CommandText = StringBuilderCache.GetStringAndRelease(sb);
            return cmd;
        }

        protected internal virtual DbCommand BuildUpdateCommand(Entity entity, EntityModel model)
        {
            var cmd = MakeCommand();
            var sb = StringBuilderCache.Acquire();
            int pindex = 0;
            EntityMemberModel mm;
            bool hasChangedMember = false;
            var tableName = model.GetSqlTableName(false, null);

            sb.Append($"Update \"{tableName}\" Set ");
            for (int i = 0; i < model.Members.Count; i++)
            {
                mm = model.Members[i];
                if (mm.Type == EntityMemberType.DataField)
                {
                    var dfm = (DataFieldModel)mm;
                    if (dfm.IsPrimaryKey) continue; //跳过主键
                    ref EntityMember m = ref entity.GetMember(dfm.MemberId);
                    if (!m.HasChanged) continue;    //没有变更

                    pindex++;
                    var para = MakeParameter();
                    para.ParameterName = $"V{pindex}";
                    if (m.HasValue)
                    {
                        switch (dfm.DataType)
                        {
                            case EntityFieldType.UInt16:
                                para.Value = unchecked((short)entity.Members[i].UInt16Value);
                                break;
                            case EntityFieldType.UInt32:
                                para.Value = unchecked((int)entity.Members[i].UInt32Value);
                                break;
                            case EntityFieldType.UInt64:
                                para.Value = unchecked((long)entity.Members[i].UInt64Value);
                                break;
                            default:
                                para.Value = entity.Members[i].BoxedValue; //TODO: no boxing
                                break;
                        }
                    }
                    else
                    {
                        //if (dfm.DataType == EntityFieldType.Binary) //why?
                        //    para.DbType = DbType.Binary;
                        para.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(para);

                    if (hasChangedMember)
                        sb.Append(",");
                    else
                        hasChangedMember = true;
                    sb.Append($"{NameEscaper}{dfm.SqlColName}{NameEscaper}=@{para.ParameterName}");
                }
            }

            //根据主键生成条件
            sb.Append(" Where");
            BuildWhereForUpdateOrDelete(entity, model, cmd, sb);

            cmd.CommandText = StringBuilderCache.GetStringAndRelease(sb);
            if (!hasChangedMember) throw new InvalidOperationException("entity has no changed member");
            return cmd;
        }

        private void BuildWhereForUpdateOrDelete(Entity entity, EntityModel model, DbCommand cmd, System.Text.StringBuilder sb)
        {
            int pindex = 0;
            FieldWithOrder pk;
            DataFieldModel mm;
            for (int i = 0; i < model.SqlStoreOptions.PrimaryKeys.Count; i++)
            {
                pk = model.SqlStoreOptions.PrimaryKeys[i];
                mm = (DataFieldModel)model.GetMember(pk.MemberId, true);

                pindex++;
                var para = MakeParameter();
                para.ParameterName = $"p{pindex}";
                para.Value = entity.GetMember(pk.MemberId).BoxedValue; //TODO: no boxing
                cmd.Parameters.Add(para);

                if (i != 0) sb.Append(" And");
                sb.Append($" {NameEscaper}{mm.SqlColName}{NameEscaper}=@{para.ParameterName}");
            }
        }

        protected internal abstract DbCommand BuildDeleteCommand(SqlDeleteCommand deleteCommand);

        /// <summary>
        /// 将SqlUpdateCommand转换为sql
        /// </summary>
        protected internal abstract DbCommand BuidUpdateCommand(SqlUpdateCommand updateCommand);

        protected internal abstract DbCommand BuildQuery(ISqlSelectQuery query);
        #endregion

        #region ====由服务调用的简化方法====
        public async Task<DbConnection> OpenConnectionAsync()
        {
            var conn = MakeConnection();
            await conn.OpenAsync();
            return conn;
        }

        public async Task SaveAsync(Entity entity, DbTransaction txn = null)
        {
            switch (entity.PersistentState)
            {
                case PersistentState.Detached:
                    await InsertAsync(entity, txn);
                    break;
                case PersistentState.Modified:
                case PersistentState.Unchanged: //TODO: remove this, test only
                    await UpdateAsync(entity, txn);
                    break;
                default:
                    throw new InvalidOperationException($"Can't save entity with state: {entity.PersistentState}");
            }
        }
        #endregion
    }

}
