using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using appbox.Models;
using appbox.Runtime;
using System.Data.Common;
using System.Data;
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
        public const string TotalRowsColumnName = "_tr";
        public const string RowNumberColumnName = "_rn";

        #region ====Statics====
        private static readonly Dictionary<ulong, SqlStore> sqlStores = new Dictionary<ulong, SqlStore>();

        /// <summary>
        /// 获取SqlStore实例，缓存不存在则创建
        /// </summary>
        public static SqlStore Get(ulong storeId)
        {
            SqlStore res;
            if (!sqlStores.TryGetValue(storeId, out res))
            {
                lock (sqlStores)
                {
                    if (!sqlStores.TryGetValue(storeId, out res))
                    {
                        //加载存储模型
                        var model = ModelStore.LoadModelAsync(storeId).Result as DataStoreModel;
                        if (model == null || model.Kind != DataStoreKind.Sql)
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
        /// 字段转义符，如PG用引号包括字段名称"xxx"
        /// </summary>
        public abstract string FieldEscaper { get; }

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
        public async Task CreateTableAsync(EntityModel model, DbTransaction txn)
        {
            Debug.Assert(txn != null);
            var cmds = MakeCreateTable(model);
            foreach (var cmd in cmds)
            {
                cmd.Connection = txn.Connection;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        protected internal abstract IList<DbCommand> MakeCreateTable(EntityModel model);

        protected internal abstract IList<DbCommand> MakeAlterTable(EntityModel model);

        protected internal abstract DbCommand MakeDropTable(EntityModel model);
        #endregion

        #region ====DML Methods====
        public virtual async Task<int> InsertAsync(Entity entity, DbTransaction txn)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.PersistentState != PersistentState.Detached)
                throw new InvalidOperationException("Can't insert none new entity.");

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(entity.ModelId);
            if (model.SqlStoreOptions == null)
                throw new InvalidOperationException("Can't insert entity to sqlstore");

            var cmd = BuildInsertCommand(entity, model);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            if (txn == null)
                await cmd.Connection.OpenAsync();
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

        public async Task ExecCommandAsync(SqlUpdateCommand updateCommand, DbTransaction txn)
        {
            //暂不支持无条件更新，以防止误操作
            if (Expressions.Expression.IsNull(updateCommand.Filter))
                throw new NotSupportedException("Update must assign Where condition");

            var cmd = BuidUpdateCommand(updateCommand);
            cmd.Connection = txn != null ? txn.Connection : MakeConnection();
            if (txn == null)
                await cmd.Connection.OpenAsync();
            //执行命令
            if (updateCommand.HasOutputItems && UseReaderForOutput) //返回字段通过DbReader读取
            {
                try
                {
                    var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
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
            //开始构建Sql
            sb.Append($"Insert Into {FieldEscaper}{model.Name}{FieldEscaper} (");
            for (int i = 0; i < entity.Members.Length; i++)
            {
                mm = model.GetMember(entity.Members[i].Id, true);
                if (mm.Type == EntityMemberType.DataField && entity.Members[i].Flag.HasValue)
                {
                    pindex++;
                    var para = MakeParameter();
                    para.ParameterName = $"V{pindex}";
                    para.Value = entity.Members[i].BoxedValue;
                    cmd.Parameters.Add(para);

                    sb.Append($"{sep}{FieldEscaper}{mm.Name}{FieldEscaper}");
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

        /// <summary>
        /// 将SqlUpdateCommand转换为sql
        /// </summary>
        protected internal abstract DbCommand BuidUpdateCommand(SqlUpdateCommand updateCommand);

        protected internal abstract DbCommand BuildQuery(ISqlSelectQuery query);
        #endregion

        #region ====由服务调用的简化方法====
        public async Task SaveAsync(Entity entity, DbTransaction txn = null)
        {
            switch (entity.PersistentState)
            {
                case PersistentState.Detached:
                    await InsertAsync(entity, txn);
                    break;
                //case PersistentState.Modified:
                //case PersistentState.Unchanged: //TODO: remove this, test only
                //    await UpdateEntityAsync(entity, txn);
                //    break;
                default:
                    throw ExceptionHelper.NotImplemented();
            }
        }
        #endregion
    }

}
