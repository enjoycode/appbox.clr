using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private readonly ObjectPool<PooledTaskSource<AnyValue>> invokeTasksPool =
            PooledTaskSource<AnyValue>.Create(256); //TODO: check count

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
        public ValueTask<AnyValue> InvokeAsync(string servicePath, InvokeArgs args)
        {
            return InvokeInternal(servicePath, args, false, 0);
        }

        /// <summary>
        /// Invoke by websocket or ajax client, free buffer after invoke system service or forward to sub process
        /// </summary>
        internal ValueTask<AnyValue> InvokeByClient(string servicePath, int msgId, InvokeArgs args)
        {
            return InvokeInternal(servicePath, args, true, msgId);
        }

        private async ValueTask<AnyValue> InvokeInternal(string servicePath, InvokeArgs args, bool byClient, int msgId)
        {
            if (string.IsNullOrEmpty(servicePath))
                throw new ArgumentNullException(nameof(servicePath));

            var span = servicePath.AsMemory();
            var firstDot = span.Span.IndexOf('.');
            var lastDot = span.Span.LastIndexOf('.');
            if (firstDot == lastDot)
                throw new ArgumentException(nameof(servicePath));
            var app = span.Slice(0, firstDot);
            var service = servicePath.AsMemory(firstDot + 1, lastDot - firstDot - 1);
            var method = servicePath.AsMemory(lastDot + 1);

            if (app.Span.SequenceEqual(appbox.Consts.SYS.AsSpan()))
            {
                if (Runtime.SysServiceContainer.TryGet(service, out IService serviceInstance))
                {
                    try
                    {
                        return await InvokeSysAsync(serviceInstance, servicePath, method, args);
                    }
                    finally
                    {
                        args.ReturnBuffer(); //注意归还缓存块 
                    }
                }
            }

            //非系统服务则包装为InvokeRequire转发至子进程处理
            var tcs = invokeTasksPool.Allocate();
            var require = new InvokeRequire(byClient ? InvokeSource.Client : InvokeSource.Host,
                byClient ? InvokeProtocol.Json : InvokeProtocol.Bin,
                tcs.GCHandlePtr, servicePath, args, msgId, RuntimeContext.Current.CurrentSession); //注意传播会话信息
            ChildProcess.AppContainer.Channel.SendMessage(ref require);
            args.ReturnBuffer(); //注意归还缓存块
            var res = await tcs.WaitAsync();
            invokeTasksPool.Free(tcs);
            return res;
        }

        /// <summary>
        /// 调用系统服务，埋点监测
        /// </summary>
        private async ValueTask<AnyValue> InvokeSysAsync(IService serviceInstance,
            string servicePath, ReadOnlyMemory<char> method, InvokeArgs args)
        {
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            var res = await serviceInstance.InvokeAsync(method, args);
            stopWatch.Stop();
            ServerMetrics.InvokeDuration.WithLabels(servicePath).Observe(stopWatch.Elapsed.TotalSeconds);
            return res;
        }
        #endregion
    }

}
