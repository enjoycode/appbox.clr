using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using appbox.Caching;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Server
{
    sealed class AppRuntimeContext : IRuntimeContext
    {
        private readonly List<ApplicationModel> apps = new List<ApplicationModel>(); //TODO:use RWLock
        private readonly LRUCache<ulong, ModelBase> models = new LRUCache<ulong, ModelBase>(128); //TODO:fix limit
        private static readonly AsyncLocal<ISessionInfo> _session = new AsyncLocal<ISessionInfo>();
        internal readonly AppServiceContainer services = new AppServiceContainer();

        public ISessionInfo CurrentSession
        {
            get { return _session.Value; }
            set { _session.Value = value; }
        }
        public string AppPath => AppContext.BaseDirectory;
        public ulong RuntimeId => 1;

        internal SharedMemoryChannel Channel;

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
            throw new NotImplementedException();
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

        public void InvalidModelsCache(string[] services, ulong[] others, bool byPublish)
        {
            //AppRuntimeContext byPublish始终为false
            if (services != null)
            {
                for (int i = 0; i < services.Length; i++)
                {
                    this.services.TryRemove(services[i]);
                }
            }

            if (others != null)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    models.TryRemove(others[i]);
                }
            }
        }
        #endregion

        #region ====Invoke====
        public async ValueTask<AnyValue> InvokeAsync(string servicePath, InvokeArgs args)
        {
            var firstDot = servicePath.IndexOf('.');
            var lastDot = servicePath.LastIndexOf('.');
            if (firstDot == lastDot)
                throw new ArgumentException(nameof(servicePath));
            var service = servicePath.Substring(0, lastDot);
            var method = servicePath.AsMemory(lastDot + 1);

            var instance = await services.TryGetAsync(service);
            if (instance == null)
                throw new Exception($"Cannot find service:{service}");

            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            var res = await instance.InvokeAsync(method, args); //ConfigureAwait(false)??
            stopWatch.Stop();
            var metricReq = new MetricRequire(servicePath, stopWatch.Elapsed.TotalSeconds);
            Channel.SendMessage(ref metricReq); //TODO:考虑调试子进程不发送

            return res;
        }
        #endregion
    }
}
