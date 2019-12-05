using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using appbox.Caching;
using appbox.Models;
using appbox.Runtime;
using appbox.Data;
using appbox.Host;

namespace appbox.Server
{

    /// <summary>
    /// 主进程运行时上下文
    /// </summary>
    sealed class HostRuntimeContext : IHostRuntimeContext
    {
        private readonly List<ApplicationModel> apps = new List<ApplicationModel>(); //TODO:use RWLock
        private readonly LRUCache<ulong, ModelBase> models = new LRUCache<ulong, ModelBase>(128); //TODO:fix limit

        private static readonly AsyncLocal<ISessionInfo> _session = new AsyncLocal<ISessionInfo>();

        public string AppPath { get; }
        public ulong RuntimeId => 0;

        public ISessionInfo CurrentSession
        {
            get { return _session.Value; }
            set { _session.Value = value; }
        }

        public HostRuntimeContext()
        {
            AppPath = Environment.CurrentDirectory;
        }

        #region ====Model Containers====
        public async ValueTask<ApplicationModel> GetApplicationModelAsync(uint appId)
        {
            for (int i = 0; i < apps.Count; i++)
            {
                if (apps[i].Id == appId)
                    return apps[i];
            }

            var appModel = await Store.ModelStore.LoadApplicationAsync(appId);
            if (appModel == null)
                return null;

            lock (apps)
            {
                bool found = false;
                for (int i = 0; i < apps.Count; i++)
                {
                    if (apps[i].Id == appId)
                    {
                        found = true; break;
                    }
                }
                if (!found)
                    apps.Add(appModel);
            }

            return appModel;
        }

        public ValueTask<ApplicationModel> GetApplicationModelAsync(string appName)
        {
            //TODO:***暂简单实现，只在已加载中查找
            for (int i = 0; i < apps.Count; i++)
            {
                if (apps[i].Name == appName)
                    return new ValueTask<ApplicationModel>(apps[i]);
            }
            throw ExceptionHelper.NotImplemented();
        }

        public async ValueTask<T> GetModelAsync<T>(ulong modelId) where T : ModelBase
        {
            if (models.TryGet(modelId, out ModelBase model))
                return (T)model;

            model = await Store.ModelStore.LoadModelAsync(modelId);
            if (model != null)
                models.TryAdd(modelId, model);

            return (T)model;
        }

        public void AddModelCache(ModelBase model)
        {
            model.AcceptChanges();
            models.TryAdd(model.Id, model);
        }

        public void InvalidModelsCache(string[] services, ulong[] others, bool byPublish)
        {
            //HostRuntimeContext无services实例需要更新
            //先通知本机所有子进程更新缓存
            ChildProcess.InvalidModelsCache(services, others);
            //再更新主进程缓存
            if (others != null)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    models.TryRemove(others[i]);
                }
            }
            //最后通知整个集群
            if (byPublish)
            {
                //TODO:***** 广播事件至集群
            }
        }
        #endregion

        #region ====Invoke Methods====
        public Task<object> InvokeAsync(string servicePath, InvokeArgs args)
        {
            return InvokeInternalAsync(InvokeSource.Host, InvokeContentType.Bin, servicePath, args, 0);
        }

        /// <summary>
        /// Invokes the by web client async.
        /// </summary>
        /// <remarks>
        /// Caller已经设置当前会话
        /// </remarks>
        internal Task<object> InvokeByWebClientAsync(string servicePath, InvokeArgs args, int msgId)
        {
            return InvokeInternalAsync(InvokeSource.Client, InvokeContentType.Json, servicePath, args, msgId);
        }

        private Task<object> InvokeInternalAsync(InvokeSource source, InvokeContentType contentType, string servicePath, InvokeArgs args, int msgId)
        {
            if (string.IsNullOrEmpty(servicePath))
                throw new ArgumentNullException(nameof(servicePath));

            var span = servicePath.AsSpan();
            var firstDot = span.IndexOf('.');
            var lastDot = span.LastIndexOf('.');
            if (firstDot == lastDot)
                throw new ArgumentException(nameof(servicePath));
            var app = span.Slice(0, firstDot);
            var service = servicePath.AsMemory(firstDot + 1, lastDot - firstDot - 1);
            var method = span.Slice(lastDot + 1);

            if (app.SequenceEqual(appbox.Consts.SYS.AsSpan()))
            {
                if (Runtime.SysServiceContainer.TryGet(service, out IService serviceInstance))
                {
                    return InvokeSysAsync(serviceInstance, servicePath, method.ToString(), args);
                }
            }

            //转发至应用子进程, 另InvokeArgs在序列化时已归还缓存
            var tcs = new TaskCompletionSource<object>();
            var tcsHandle = GCHandle.Alloc(tcs);
            var require = new InvokeRequire(source, contentType, GCHandle.ToIntPtr(tcsHandle),
                servicePath, args, msgId, RuntimeContext.Current.CurrentSession); //注意传播会话信息
            ChildProcess.AppContainer.Channel.SendMessage(ref require);
            return tcs.Task;
        }

        /// <summary>
        /// 调用系统服务，埋点监测
        /// </summary>
        private async Task<object> InvokeSysAsync(IService serviceInstance, string servicePath, string method, InvokeArgs args)
        {
            args.BeginGet();
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            var res = await serviceInstance.InvokeAsync(method, args);
            stopWatch.Stop();
            ServerMetrics.InvokeDuration.WithLabels(servicePath).Observe(stopWatch.Elapsed.TotalSeconds);
            return res;
        }
        #endregion
    }

}
