﻿#if !FUTURE

using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 模型存储相关Api
    /// </summary>
    internal static class ModelStore
    {
        private const int APP_DATA_OFFSET = 9;

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

        internal static unsafe object DeserializeModel(IntPtr dataPtr, int dataSize, int offset = 0)
        {
            byte* dp = (byte*)dataPtr.ToPointer();

            object result = null;
            var stream = new UnmanagedMemoryStream(dp + offset, dataSize - offset);
            BinSerializer cf = new BinSerializer(stream);
            try { result = cf.Deserialize(); }
            catch (Exception) { throw; }
            finally { cf.Clear(); }

            stream.Close();

            if (result != null && result is ApplicationModel && offset == APP_DATA_OFFSET)
            {
                var app = (ApplicationModel)result;
                app.StoreId = dp[0];
                uint* devIdCounterPtr = (uint*)(dp + 1);
                //uint* usrIdCounterPtr = (uint*)(dp + 5);
                app.DevModelIdSeq = *devIdCounterPtr;
            }
            return result;
        }

        internal static async ValueTask CreateApplicationAsync(ApplicationModel app
#if !FUTURE
            , DbTransaction txn
#endif
            )
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            BuildInsertMetaCommand(cmd, Meta_Application, app.Id.ToString(), ModelType.Application, SerializeModel(app), false);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask<ulong> GenModelIdAsync(uint appId, ModelType type, ModelLayer layer)
        {
            throw new NotImplementedException();
            //if (layer == ModelLayer.SYS) //不允许SYS Layer
            //    throw new ArgumentException(nameof(layer));

            //var seq = await HostApi.MetaGenModelIdAsync(appId, layer == ModelLayer.DEV);

            //var nid = (ulong)appId << IdUtil.MODELID_APPID_OFFSET;
            //nid |= (ulong)type << IdUtil.MODELID_TYPE_OFFSET;
            //nid |= (ulong)seq << IdUtil.MODELID_SEQ_OFFSET;
            //nid |= (ulong)layer;
            //return nid;
        }

        /// <summary>
        /// 用于运行时加载单个ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel> LoadApplicationAsync(uint appId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 用于设计时加载所有ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel[]> LoadAllApplicationAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 用于设计时加载所有Model
        /// </summary>
        internal static async ValueTask<ModelBase[]> LoadAllModelAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 用于设计时加载所有Folder
        /// </summary>
        internal static async ValueTask<ModelFolder[]> LoadAllFolderAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 加载单个Model，用于运行时或设计时重新加载
        /// </summary>
        internal static async ValueTask<ModelBase> LoadModelAsync(ulong modelId)
        {
            throw new NotImplementedException();
        }

        internal static async ValueTask InsertModelAsync(ModelBase model, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            BuildInsertMetaCommand(cmd, Meta_Model, model.Id.ToString(), model.ModelType, SerializeModel(model), false);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask UpdateModelAsync(ModelBase model, DbTransaction txn, Func<uint, ApplicationModel> getApp)
        {
            throw new NotImplementedException();
        }

        internal static async ValueTask DeleteModelAsync(ModelBase model, DbTransaction txn, Func<uint, ApplicationModel> getApp)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 插入或更新文件夹
        /// </summary>
        internal static async ValueTask UpsertFolderAsync(ModelFolder folder, DbTransaction txn)
        {
            if (folder.Parent != null)
                throw new InvalidOperationException("Can't save none root folder.");

            //TODO:暂先删除再插入
            var id = folder.AppId.ToString(); //只需要AppId，RootFolder.Id=Guid.Empty
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
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
            BuildDeleteMetaCommand(cmd, Meta_Code, id);
            BuildInsertMetaCommand(cmd, Meta_Code, id, ModelType.Application/**/, codeData, true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteModelCodeAsync(ulong modelId, DbTransaction txn)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 仅用于加载服务模型的代码
        /// </summary>
        internal static async ValueTask<string> LoadServiceCodeAsync(ulong modelId)
        {
            var res = await LoadModelCodeAsync(modelId);
            if (res == null)
                return null;

            ModelCodeUtil.DecodeServiceCode(res.DataPtr, (int)res.Size, out string sourceCode, out string declareCode);
            res.Dispose();
            return sourceCode;
        }

        /// <summary>
        /// 仅用于加载视图模型的代码
        /// </summary>
        internal static async ValueTask<ValueTuple<string, string, string>> LoadViewCodeAsync(ulong modelId)
        {
            var res = await LoadModelCodeAsync(modelId);
            if (res == null)
                return ValueTuple.Create<string, string, string>(null, null, null);

            ModelCodeUtil.DecodeViewCode(res.DataPtr, (int)res.Size, out string templateCode, out string scriptCode, out string styleCode);
            res.Dispose();
            return ValueTuple.Create(templateCode, scriptCode, styleCode);
        }

        private static ValueTask<INativeData> LoadModelCodeAsync(ulong modelId)
        {
            throw new NotImplementedException();
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
            BuildDeleteMetaCommand(cmd, Meta_Code, asmName);
            BuildInsertMetaCommand(cmd, Meta_Code, asmName, model, asmData, true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteAssemblyAsync(bool isService, string asmName, DbTransaction txn)
        {
            throw new NotImplementedException();
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
            var res = await LoadAssemblyAsync(true, serviceName);
            if (res == null)
                return null;

            var data = new byte[res.Size];
            unsafe
            {
                fixed (byte* pd = data)
                {
                    Buffer.MemoryCopy(res.DataPtr.ToPointer(), pd, (int)res.Size, (int)res.Size);
                }
            }
            res.Dispose();
            return data;
        }

        internal static async ValueTask<string> LoadViewAssemblyAsync(string viewName)
        {
            var res = await LoadAssemblyAsync(false, viewName);
            if (res == null)
                return null;

            ModelCodeUtil.DecodeViewRuntimeCode(res.DataPtr, (int)res.Size, out string runtimeCode);
            res.Dispose();
            return runtimeCode;
        }

        private static ValueTask<INativeData> LoadAssemblyAsync(bool isService, string asmName)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ====视图模型路由相关====
        /// <summary>
        /// 保存视图模型路由表
        /// </summary>
        /// <param name="viewName">eg: sys.CustomerList</param>
        /// <param name="path">无自定义路由为空</param>
        internal static async ValueTask UpsertViewRoute(string viewName, string path, DbTransaction txn)
        {
            using var cmd = SqlStore.Default.MakeCommand();
            cmd.Connection = txn.Connection;
            BuildDeleteMetaCommand(cmd, Meta_View_Router, viewName);
            BuildInsertMetaCommand(cmd, Meta_View_Router, viewName, ModelType.View, System.Text.Encoding.UTF8.GetBytes(path), true);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static async ValueTask DeleteViewRoute(string viewName, DbTransaction txn)
        {
            throw new NotImplementedException();
        }

        internal static async ValueTask<ValueTuple<string, string>[]> LoadViewRoutes()
        {
            throw new NotImplementedException();
        }
        #endregion

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

        private static void BuildDeleteMetaCommand(DbCommand cmd, byte metaType, string id)
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            cmd.CommandText = $"Delete From {esc}sys.Meta{esc} Where meta={metaType} And id='{id}'";
        }
    }
}

#endif