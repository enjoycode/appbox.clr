#if !FUTURE

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

        #region ====初始化====
        /// <summary>
        /// 如果没有初始化则创建元数据表结构
        /// </summary>
        internal static void TryInitMetaStore()
        {
            var db = SqlStore.Default;
            var esc = db.NameEscaper;
            //暂通过查询判断有无初始化过
            using var cmd1 = db.MakeCommand();
            cmd1.CommandText = $"Select {esc}MetaType{esc} From {esc}sys.Meta{esc} Where {esc}MetaType{esc}={Meta_Application} And {esc}Id{esc}='{Consts.SYS_APP_ID.ToString()}'";
            using var conn = db.MakeConnection();
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Log.Warn($"Open sql connection error: {ex.Message}");
                Environment.Exit(0);
            }

            cmd1.Connection = conn;
            try
            {
                using var dr = cmd1.ExecuteReader();
                return;
            }
            catch (Exception ex)
            {
                Log.Debug($"CMD:{cmd1.CommandText} MSG:{ex.Message}");
                Log.Info("Start create meta store...");
            }

            using var cmd2 = db.MakeCommand();
            cmd2.CommandText = $"Create Table {esc}sys.Meta{esc} ({esc}MetaType{esc} smallint NOT NULL, {esc}Id{esc} varchar(100) NOT NULL, {esc}ModelType{esc} smallint, {esc}Data{esc} {db.BlobType} NOT NULL);";
            cmd2.CommandText += $"Alter Table {esc}sys.Meta{esc} Add CONSTRAINT {esc}PK_Meta{esc} Primary Key ({esc}MetaType{esc},{esc}Id{esc});";
            cmd2.Connection = conn;
            try
            {
                cmd2.ExecuteNonQuery();
                Log.Info("Create meta store done.");
            }
            catch (Exception ex)
            {
                Log.Warn($"Create meta store error: {ex.Message}");
                Environment.Exit(0);
            }
        }
        #endregion

        #region ====模型相关操作====
        /// <summary>
        /// Serializes the model.
        /// </summary>
        /// <returns>在主进程内返回的是NativeString，在子进程内返回NativeBytes</returns>
        internal static unsafe IntPtr SerializeModel(object obj, out int size, int offset = 0)
        {
            //TODO:暂使用内存Copy
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream(1024))
            {
                BinSerializer cf = new BinSerializer(ms);
                try { cf.Serialize(obj); }
                catch (Exception) { throw; }
                finally { cf.Clear(); }

                ms.Close();
                data = ms.ToArray();
            }

            size = data.Length + offset;
            IntPtr nativeDataPtr = IntPtr.Zero;
            byte* dp;
            if (Runtime.RuntimeContext.Current.RuntimeId == 0)
            {
                nativeDataPtr = NativeApi.NewNativeString(size, out dp);
            }
            else
            {
                nativeDataPtr = NativeBytes.MakeRaw(size);
                dp = (byte*)nativeDataPtr + 4;
            }

            //Log.Debug($"序列化模型: {obj.GetType().Name} arraySize={size} stringSize={NativeString.GetSize(dataString)}");
            fixed (byte* pv = data)
            {
                for (int i = 0; i < offset; i++)
                {
                    dp[i] = 0;
                }
                Buffer.MemoryCopy(pv, dp + offset, data.Length, data.Length);
            }

            return nativeDataPtr;
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

        internal static async ValueTask CreateApplicationAsync(ApplicationModel app)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

            throw new NotImplementedException();
        }
        #endregion

        #region ====模型代码及Assembly相关操作====
        /// <summary>
        /// Insert or Update模型相关的代码，目前主要用于服务模型及视图模型
        /// </summary>
        /// <param name="codeData">已经压缩编码过</param>
        internal static async ValueTask UpsertModelCodeAsync(ulong modelId, byte[] codeData, DbTransaction txn)
        {
            throw new NotImplementedException();
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
        internal static ValueTask UpsertAssemblyAsync(bool isService, string asmName, byte[] asmData, DbTransaction txn)
        {
            throw new NotImplementedException();
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
        internal static ValueTask UpsertViewRoute(string viewName, string path, DbTransaction txn)
        {
            throw new NotImplementedException();
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
    }
}

#endif