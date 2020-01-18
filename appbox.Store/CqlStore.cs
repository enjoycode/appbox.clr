using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Store
{
    public abstract class CqlStore
    {
        #region ====Statics====
        private static readonly Dictionary<ulong, CqlStore> cqlStores = new Dictionary<ulong, CqlStore>();

#if !FUTURE
        //internal static readonly ulong DefaultCqlStoreId = unchecked((ulong)StringHelper.GetHashCode("Default"));

        //internal static CqlStore Default { get; private set; }

        //internal static void SetDefaultCqlStore(CqlStore defaultCqlStore)
        //{
        //    Debug.Assert(defaultCqlStore != null);
        //    cqlStores.Add(DefaultCqlStoreId, defaultCqlStore);
        //    Default = defaultCqlStore;
        //}
#endif

        /// <summary>
        /// 获取CqlStore实例，缓存不存在则创建
        /// </summary>
        public static CqlStore Get(ulong storeId)
        {
            if (!cqlStores.TryGetValue(storeId, out CqlStore res))
            {
                lock (cqlStores)
                {
                    if (!cqlStores.TryGetValue(storeId, out res))
                    {
                        //加载存储模型
                        if (!(ModelStore.LoadModelAsync(storeId).Result is DataStoreModel model)
                            || model.Kind != DataStoreKind.Cql)
                            throw new Exception($"Can't get CqlStore[Id={storeId}]");

                        //根据Provider创建实例
                        var ps = model.Provider.Split(';');
                        var asmPath = Path.Combine(RuntimeContext.Current.AppPath, Server.Consts.LibPath, ps[0] + ".dll");
                        try
                        {
                            var asm = Assembly.LoadFile(asmPath);
                            var type = asm.GetType(ps[1]);
                            res = (CqlStore)Activator.CreateInstance(type, model.Settings);
                            cqlStores[storeId] = res;
                            Log.Debug($"Create CqlStore instance: {type}, isNull={res == null}");
                            return res;
                        }
                        catch (Exception ex)
                        {
                            var error = $"Create CqlStore[Provider={model.Provider}] instance error: {ex.Message}";
                            throw new Exception(error);
                        }
                    }
                }
            }
            return res;
        }
        #endregion

        #region ====DML & Execute Methods====
        public abstract Task InsertAsync(Entity entity, bool ifNotExists = false);

        public abstract Task UpdateAsync(Entity entity, bool ifNotExists = false);

        public abstract Task DeleteAsync(Entity entity, bool ifNotExists = false);

        public abstract Task<IRowSet> ExecuteAsync(string cql);

        public abstract Task<IRowSet> ExecuteAsync(ref CqlBatch batch);
        #endregion

        #region ====DDL Methods====
        public abstract Task CreateTableAsync(EntityModel model);

        public abstract Task AlterTableAsync(EntityModel model);

        public abstract Task DropTableAsync(EntityModel model);
        #endregion
    }
}
