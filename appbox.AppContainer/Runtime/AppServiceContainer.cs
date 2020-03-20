using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using appbox.Runtime;

namespace appbox.Server
{

    sealed class AppServiceContainer
    {
        private struct ServiceInfo
        {
            public IService Instance;
            public ServiceAssemblyLoader Loader;
        }

        //TODO:use LRUCache
        private readonly Dictionary<string, ServiceInfo> services = new Dictionary<string, ServiceInfo>(100);

        /// <summary>
        /// 根据名称获取运行时服务实例
        /// </summary>
        /// <param name="name">eg:sys.HelloService</param>
        public async ValueTask<IService> TryGetAsync(string name)
        {
            if (services.TryGetValue(name, out ServiceInfo service))
                return service.Instance;

            //加载服务模型的组件
            var asmData = await Store.ModelStore.LoadServiceAssemblyAsync(name);
            if (asmData == null || asmData.Length == 0)
            {
                Log.Warn($"无法从存储加载ServiceAssembly: {name}");
                return null;
            }
            //释放应用的第三方组件为临时文件，因非托管组件只能从文件加载
            //TODO:避免重复释放或者考虑获取服务模型后根据引用释放
            var dotIndex = name.AsSpan().IndexOf('.');
            var appName = name.AsSpan(0, dotIndex).ToString();
            var libPath = Path.Combine(Consts.LibPath, appName);
            await Store.ModelStore.ExtractAppAssemblies(appName, libPath);

            lock (services)
            {
                if (!services.TryGetValue(name, out service))
                {
                    var asmLoader = new ServiceAssemblyLoader(libPath);
                    var asm = asmLoader.LoadServiceAssembly(asmData);
                    var sr = name.Split('.');
                    var type = asm.GetType($"{sr[0]}.ServiceLogic.{sr[1]}", true);
                    var instance = (IService)Activator.CreateInstance(type);
                    service = new ServiceInfo { Instance = instance, Loader = asmLoader };
                    services.TryAdd(name, service);
                    Log.Debug($"加载服务实例: {asm.FullName}");
                }
            }
            return service.Instance;
        }

        /// <summary>
        /// 预先注入调试目标服务实例，防止从存储加载
        /// </summary>
        internal void InjectDebugService(ulong debugSessionId)
        {
            var debugFolder = Path.Combine(AppContext.BaseDirectory, "debug", debugSessionId.ToString());
            if (!Directory.Exists(debugFolder))
            {
                Log.Warn("Start debug process can't found target folder.");
                return;
            }

            var files = Directory.GetFiles(debugFolder);
            var asmLoader = new ServiceAssemblyLoader(Path.Combine(debugFolder, "lib"));
            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetExtension(files[i]) == ".dll")
                {
                    var sr = Path.GetFileName(files[i]).Split('.');
                    var asm = asmLoader.LoadFromAssemblyPath(files[i]);
                    var type = asm.GetType($"{sr[0]}.ServiceLogic.{sr[2]}", true);
                    var instance = (IService)Activator.CreateInstance(type);
                    services.TryAdd($"{sr[0]}.{sr[2]}", new ServiceInfo { Instance = instance, Loader = asmLoader });
                    Log.Debug("Inject debug service instance:" + files[i]);
                }
            }
        }

        /// <summary>
        /// 主要用于热更新时移除旧的服务实例
        /// </summary>
        public bool TryRemove(string name)
        {
            lock (services)
            {
                if (services.TryGetValue(name, out ServiceInfo service))
                {
                    services.Remove(name);
                    //#if DEBUG
                    //                    service.Loader.Unloading += OnUnloading;
                    //#endif
                    service.Loader.Unload();
                    //#if DEBUG
                    //                    service.Loader.Unloading -= OnUnloading;
                    //#endif
                }
            }
            return true;
        }

        //#if DEBUG
        //        private static void OnUnloading(System.Runtime.Loader.AssemblyLoadContext context)
        //        {
        //            var sb = Caching.StringBuilderCache.Acquire();
        //            sb.AppendLine("Unloading service assemblies:");
        //            foreach (var asm in context.Assemblies)
        //            {
        //                sb.AppendLine(asm.FullName);
        //            }
        //            Log.Warn(Caching.StringBuilderCache.GetStringAndRelease(sb));
        //        }
        //#endif

    }
}
