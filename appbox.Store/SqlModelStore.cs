#if !FUTURE

using System;
using System.Data.Common;
using System.Threading.Tasks;
using appbox.Models;

namespace appbox.Store
{
    internal static class ModelStore
    {
        internal static void TryInit()
        {

        }

        #region ====模型相关操作====
        internal static async ValueTask CreateApplicationAsync(ApplicationModel app)
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

        /// <summary>
        /// 插入或更新文件夹
        /// </summary>
        internal static async ValueTask UpsertFolderAsync(ModelFolder folder, DbTransaction txn)
        {
            throw new NotImplementedException();
        }

        internal static async ValueTask InsertModelAsync(ModelBase model, DbTransaction txn)
        {
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

        /// <summary>
        /// 保存编译好的服务组件或视图运行时代码
        /// </summary>
        /// <param name="asmName">eg: sys.HelloService or sys.CustomerView</param>
        internal static ValueTask UpsertAssemblyAsync(bool isService, string asmName, byte[] asmData, DbTransaction txn)
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
            throw new NotImplementedException();
        }
        #endregion
    }
}

#endif