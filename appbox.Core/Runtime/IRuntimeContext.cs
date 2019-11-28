using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;

namespace appbox.Runtime
{

    /// <summary>
    /// 运行时上下文，用于提供模型容器及服务调用
    /// </summary>
    public interface IRuntimeContext
    {

        #region ====Properties====
        /// <summary>
        /// 当前用户的会话信息，可能返回NULL
        /// </summary>
        ISessionInfo CurrentSession { get; set; }

        //IEventManager EventManager { get; }

        /// <summary>
        /// 运行时根目录，单元测试需要
        /// </summary>
        string AppPath { get; }

        ulong RuntimeId { get; }
        #endregion

        #region ====ModelContainer====
        ValueTask<ApplicationModel> GetApplicationModelAsync(uint appId);

        ValueTask<ApplicationModel> GetApplicationModelAsync(string appName);

        ValueTask<T> GetModelAsync<T>(ulong modelId) where T : ModelBase;

        /// <summary>
        /// 用于发布时更新模型缓存
        /// </summary>
        void InvalidModelsCache(string[] services, ulong[] others, bool byPublish);
        #endregion

        #region ====Invoke Methods====
        Task<object> InvokeAsync(string servicePath, InvokeArgs args);
        #endregion

        #region ====Event Methods====
        //        /// <summary>
        //        /// 添加事件订阅者
        //        /// </summary>
        //        /// <param name="subcriber"></param>
        //        void AddEventSubcriber(EventSubcriber subcriber);
        //
        //        /// <summary>
        //        /// 移除事件订阅者
        //        /// </summary>
        //        /// <param name="subcriber"></param>
        //        void RemoveEventSubcriber(EventSubcriber subcriber);
        //
        //        /// <summary>
        //        /// 激发事件
        //        /// </summary>
        //        /// <param name="eventID"></param>
        //        /// <param name="args"></param>
        //        void RaiseEvent(string eventID, object args);
        //
        #endregion

    }

    public interface IHostRuntimeContext : IRuntimeContext
    {
        /// <summary>
        /// 仅用于初始化存储时将模型添加至缓存
        /// </summary>
        void AddModelCache(ModelBase model);
    }

}

