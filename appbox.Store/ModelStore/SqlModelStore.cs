#if !FUTURE

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Store
{
    /// <summary>
    /// 模型存储相关Api
    /// </summary>
    internal static class ModelStore
    {

        private const byte Meta_App_Model_Dev_Counter = 0xAC;
        private const byte Meta_App_Model_Usr_Counter = 0xAD;
        private const byte Meta_Application = 0x0C;
        private const byte Meta_Model = 0x0D;
        private const byte Meta_Code = 0x0E;
        private const byte Meta_Folder = 0x0F;
        private const byte Meta_Service_Assembly = 0xA0;
        private const byte Meta_View_Assembly = 0xA1;
        private const byte Meta_View_Router = 0xA2;

        #region ====初始化====
        /// <summary>
        /// 如果没有初始化则创建元数据表结构
        /// </summary>
        internal static async Task TryInitStoreAsync()
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            //暂通过查询判断有无初始化过
            using var cmd1 = db.MakeCommand();
            cmd1.CommandText = $"Select meta From {esc}sys.Meta{esc} Where meta={Meta_Application} And id='{Consts.SYS_APP_ID.ToString()}'";
            using var conn = db.MakeConnection();
            try
            {
                await conn.OpenAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Open sql connection error: {ex.Message}");
                Environment.Exit(0);
            }

            cmd1.Connection = conn;
            try
            {
                using var dr = await cmd1.ExecuteReaderAsync();
                return;
            }
            catch (Exception ex)
            {
                Log.Debug($"CMD:{cmd1.CommandText} MSG:{ex.Message}");
                Log.Info("Start create meta store...");
            }

            //开始事务初始化
            using var txn = conn.BeginTransaction();
            using var cmd2 = db.MakeCommand();
            cmd2.CommandText = $"Create Table {esc}sys.Meta{esc} (meta smallint NOT NULL, id varchar(100) NOT NULL, model smallint, data {db.BlobType} NOT NULL);";
            cmd2.CommandText += $"Alter Table {esc}sys.Meta{esc} Add CONSTRAINT {esc}PK_Meta{esc} Primary Key (meta,id);";
            cmd2.Connection = conn;
            cmd2.Transaction = txn;
            try
            {
                await cmd2.ExecuteNonQueryAsync();
                Log.Info("Create meta table done.");
                await StoreInitiator.InitAsync(txn);
                txn.Commit();
                Log.Info("Init default sql store done.");
            }
            catch (Exception ex)
            {
                Log.Warn($"Init default sql store error: {ex.GetType().Name}\n{ex.Message}\n{ex.StackTrace}");
                Environment.Exit(0); //TODO:退出前关闭子进程
            }
        }
        #endregion

        #region ====模型相关操作====
        /// <summary>
        /// Serializes the model.
        /// </summary>
        internal static byte[] SerializeModel(object obj)
        {
            using var ms = new MemoryStream(1024);
            BinSerializer cf = new BinSerializer(ms);
            try { cf.Serialize(obj); }
            catch (Exception) { throw; }
            finally { cf.Clear(); }

            ms.Close();
            return ms.ToArray();
        }

        internal static object DeserializeModel(byte[] data)
        {
            object result = null;

            using var ms = new MemoryStream(data);
            BinSerializer cf = new BinSerializer(ms);
            try { result = cf.Deserialize(); }
            catch (Exception) { throw; }
            finally { cf.Clear(); }

            return result;
        }

        internal static async ValueTask CreateApplicationAsync(ApplicationModel app)
        {
            using var conn = SqlStore.Default.MakeConnection();
            await conn.OpenAsync();
            using var txn = conn.BeginTransaction();
            await CreateApplicationAsync(app, txn);
            txn.Commit();
        }

        internal static async ValueTask CreateApplicationAsync(ApplicationModel app, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildInsertMetaCommand(cmd, Meta_Application, app.Id.ToString(), ModelType.Application, SerializeModel(app), false);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask<ulong> GenModelIdAsync(uint appId, ModelType type, ModelLayer layer)
        {
            if (layer == ModelLayer.SYS) //不允许SYS Layer
                throw new ArgumentException(nameof(layer));
            var meta = layer == ModelLayer.DEV ? Meta_App_Model_Dev_Counter : Meta_App_Model_Usr_Counter;
            var id = appId.ToString();

            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            using var txn = conn.BeginTransaction(); //不支持select for update的事务隔离级别

            using var cmd1 = db.MakeCommand();
            cmd1.Connection = txn.Connection;
            cmd1.Transaction = txn;
            cmd1.CommandText = $"Select data From {esc}sys.Meta{esc} Where meta={meta} And id='{id}' For Update";
            using var reader = await cmd1.ExecuteReaderAsync();
            byte[] counterData = null;
            uint seq = 0;

            using var cmd2 = db.MakeCommand();
            if (await reader.ReadAsync()) //已存在计数器
            {
                counterData = (byte[])reader.GetValue(0);
                seq = BitConverter.ToUInt32(counterData) + 1; //TODO:判断溢出
                counterData = BitConverter.GetBytes(seq);

                BuildUpdateMetaCommand(cmd2, meta, id, counterData);
            }
            else //不存在计数器
            {
                seq = 1;
                counterData = BitConverter.GetBytes(seq);
                BuildInsertMetaCommand(cmd2, meta, id, ModelType.Application, counterData, false);
            }
            reader.Close();

            cmd2.Connection = txn.Connection;
            cmd2.Transaction = txn;
            await cmd2.ExecuteNonQueryAsync();

            txn.Commit();

            var nid = (ulong)appId << IdUtil.MODELID_APPID_OFFSET;
            nid |= (ulong)type << IdUtil.MODELID_TYPE_OFFSET;
            nid |= (ulong)seq << IdUtil.MODELID_SEQ_OFFSET;
            nid |= (ulong)layer;
            return nid;
        }

        /// <summary>
        /// 用于运行时加载单个ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel> LoadApplicationAsync(uint appId)
        {
            var data = await LoadMetaDataAsync(Meta_Application, appId.ToString());
            return (ApplicationModel)DeserializeModel(data);
        }

        /// <summary>
        /// 用于设计时加载所有ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel[]> LoadAllApplicationAsync()
        {
            return await LoadMetasAsync<ApplicationModel>(Meta_Application);
        }

        /// <summary>
        /// 用于设计时加载所有Model
        /// </summary>
        internal static async ValueTask<ModelBase[]> LoadAllModelAsync()
        {
            var res = await LoadMetasAsync<ModelBase>(Meta_Model);
            for (int i = 0; i < res.Length; i++) //暂循环转换状态
            {
                res[i].AcceptChanges();
            }
            return res;
        }

        /// <summary>
        /// 用于设计时加载所有Folder
        /// </summary>
        internal static async ValueTask<ModelFolder[]> LoadAllFolderAsync()
        {
            return await LoadMetasAsync<ModelFolder>(Meta_Folder);
        }

        /// <summary>
        /// 加载单个Model，用于运行时或设计时重新加载
        /// </summary>
        internal static async ValueTask<ModelBase> LoadModelAsync(ulong modelId)
        {
            var data = await LoadMetaDataAsync(Meta_Model, modelId.ToString());
            var model = (ModelBase)DeserializeModel(data);
            model.AcceptChanges();
            return model;
        }

        internal static async ValueTask InsertModelAsync(ModelBase model, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildInsertMetaCommand(cmd, Meta_Model, model.Id.ToString(), model.ModelType, SerializeModel(model), false);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask UpdateModelAsync(ModelBase model, DbTransaction txn, Func<uint, ApplicationModel> getApp)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildUpdateMetaCommand(cmd, Meta_Model, model.Id.ToString(), SerializeModel(model));
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteModelAsync(ModelBase model, DbTransaction txn, Func<uint, ApplicationModel> getApp)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_Model, model.Id.ToString());
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 插入或更新文件夹
        /// </summary>
        internal static async ValueTask UpsertFolderAsync(ModelFolder folder, DbTransaction txn)
        {
            if (folder.Parent != null)
                throw new InvalidOperationException("Can't save none root folder.");

            //TODO:暂先删除再插入
            var id = $"{folder.AppId.ToString()}.{(byte)folder.TargetModelType}"; //RootFolder.Id=Guid.Empty
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_Folder, id);
            BuildInsertMetaCommand(cmd, Meta_Folder, id, ModelType.Folder, SerializeModel(folder), true);
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region ====模型代码及Assembly相关操作====
        /// <summary>
        /// Insert or Update模型相关的代码，目前主要用于服务模型及视图模型
        /// </summary>
        /// <param name="codeData">已经压缩编码过</param>
        internal static async ValueTask UpsertModelCodeAsync(ulong modelId, byte[] codeData, DbTransaction txn)
        {
            //TODO:暂先删除再插入
            var id = modelId.ToString();
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_Code, id);
            BuildInsertMetaCommand(cmd, Meta_Code, id, ModelType.Application/**/, codeData, true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteModelCodeAsync(ulong modelId, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_Code, modelId.ToString());
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 仅用于加载服务模型的代码
        /// </summary>
        internal static async ValueTask<string> LoadServiceCodeAsync(ulong modelId)
        {
            var data = await LoadMetaDataAsync(Meta_Code, modelId.ToString());
            if (data == null)
                return null;
            ModelCodeUtil.DecodeServiceCode(data, out string sourceCode, out _);
            return sourceCode;
        }

        /// <summary>
        /// 仅用于加载视图模型的代码
        /// </summary>
        internal static async ValueTask<ValueTuple<string, string, string>> LoadViewCodeAsync(ulong modelId)
        {
            var data = await LoadMetaDataAsync(Meta_Code, modelId.ToString());
            if (data == null)
                return ValueTuple.Create<string, string, string>(null, null, null);

            ModelCodeUtil.DecodeViewCode(data, out string templateCode, out string scriptCode, out string styleCode);
            return ValueTuple.Create(templateCode, scriptCode, styleCode);
        }

        /// <summary>
        /// 保存编译好的服务组件或视图运行时代码
        /// </summary>
        /// <param name="asmName">eg: sys.HelloService or sys.CustomerView</param>
        internal static async ValueTask UpsertAssemblyAsync(bool isService, string asmName, byte[] asmData, DbTransaction txn)
        {
            var meta = isService ? Meta_Service_Assembly : Meta_View_Assembly;
            var model = isService ? ModelType.Service : ModelType.View;
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, meta, asmName);
            BuildInsertMetaCommand(cmd, meta, asmName, model, asmData, true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteAssemblyAsync(bool isService, string asmName, DbTransaction txn)
        {
            var meta = isService ? Meta_Service_Assembly : Meta_View_Assembly;
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, meta, asmName);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 加载服务组件
        /// </summary>
        /// <param name="serviceName">eg: sys.HelloService</param>
        /// <returns></returns>
        internal static async ValueTask<byte[]> LoadServiceAssemblyAsync(string serviceName) //TODO:考虑保存至本地文件，返回路径
        {
#if !DEBUG
            //注意：只允许服务域调用
            //if (Runtime.RuntimeContext.Current.RuntimeId == 0)
            //    throw new NotSupportedException("不支持服务端主进程加载");
#endif
            return await LoadMetaDataAsync(Meta_Service_Assembly, serviceName);
        }

        internal static async ValueTask<string> LoadViewAssemblyAsync(string viewName)
        {
            var res = await LoadMetaDataAsync(Meta_View_Assembly, viewName);
            if (res == null)
                return null;

            ModelCodeUtil.DecodeViewRuntimeCode(res, out string runtimeCode);
            return runtimeCode;
        }
        #endregion

        #region ====视图模型路由相关====
        /// <summary>
        /// 保存视图模型路由表
        /// </summary>
        /// <param name="viewName">eg: sys.CustomerList</param>
        /// <param name="path">无自定义路由为空, 有上级则;分隔</param>
        internal static async ValueTask UpsertViewRoute(string viewName, string path, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_View_Router, viewName);
            BuildInsertMetaCommand(cmd, Meta_View_Router, viewName, ModelType.View, System.Text.Encoding.UTF8.GetBytes(path), true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteViewRoute(string viewName, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            cmd.Transaction = txn;
            BuildDeleteMetaCommand(cmd, Meta_View_Router, viewName);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask<ValueTuple<string, string>[]> LoadViewRoutes()
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            using var cmd = db.MakeCommand();
            cmd.Connection = conn;
            cmd.CommandText = $"Select id,data From {esc}sys.Meta{esc} Where meta={Meta_View_Router}";
            Log.Debug(cmd.CommandText);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<ValueTuple<string, string>>();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString(0);
                var valueData = (byte[])reader.GetValue(1);
                var res = ValueTuple.Create(key, System.Text.Encoding.UTF8.GetString(valueData));
                list.Add(res);
            }
            return list.ToArray();
        }
        #endregion

        #region ====Helpers====
        private static async Task<byte[]> LoadMetaDataAsync(byte metaType, string id)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            using var cmd = db.MakeCommand();
            cmd.Connection = conn;
            cmd.CommandText = $"Select data From {esc}sys.Meta{esc} Where meta={metaType} And id='{id}'";
            Log.Debug(cmd.CommandText);
            using var reader = await cmd.ExecuteReaderAsync();
            byte[] metaData = null;
            if (await reader.ReadAsync())
            {
                metaData = (byte[])reader.GetValue(0); //TODO:暂使用GetValue
            }
            return metaData;
        }

        private static async Task<T[]> LoadMetasAsync<T>(byte metaType)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            using var conn = db.MakeConnection();
            await conn.OpenAsync();
            using var cmd = db.MakeCommand();
            cmd.Connection = conn;
            cmd.CommandText = $"Select data From {esc}sys.Meta{esc} Where meta={metaType}";
            Log.Debug(cmd.CommandText);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<T>();
            while (await reader.ReadAsync())
            {
                var meta = (T)DeserializeModel((byte[])reader.GetValue(0)); //TODO:暂使用GetValue
                list.Add(meta);
            }
            return list.ToArray();
        }

        private static void BuildInsertMetaCommand(DbCommand cmd, byte metaType, string id, ModelType modelType, byte[] data, bool append)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            var pname = db.ParameterName;
            var cmdTxt = $"Insert Into {esc}sys.Meta{esc} (meta,id,model,data) Values ({metaType},'{id}',{(byte)modelType}, {pname}v)";
            if (append)
                cmd.CommandText += ";" + cmdTxt + ";";
            else
                cmd.CommandText = cmdTxt;

            var vp = db.MakeParameter();
            vp.ParameterName = $"{db.ParameterName}v";
            vp.DbType = System.Data.DbType.Binary;
            vp.Value = data;
            cmd.Parameters.Add(vp);
        }

        private static void BuildUpdateMetaCommand(DbCommand cmd, byte metaType, string id, byte[] data)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            var pname = db.ParameterName;
            cmd.CommandText = $"Update {esc}sys.Meta{esc} Set data=@v Where meta={metaType} And id='{id}'";

            var vp = db.MakeParameter();
            vp.ParameterName = $"{pname}v";
            vp.DbType = System.Data.DbType.Binary;
            vp.Value = data;
            cmd.Parameters.Add(vp);
        }

        private static void BuildDeleteMetaCommand(DbCommand cmd, byte metaType, string id)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            cmd.CommandText = $"Delete From {esc}sys.Meta{esc} Where meta={metaType} And id='{id}'";
        }
        #endregion
    }
}

#endif