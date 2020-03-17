using System;
using appbox.Runtime;
using appbox.Caching;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;

namespace appbox.Server
{
    sealed class AppServiceContainer
    {

        private readonly LRUCache<string, IService> services = new LRUCache<string, IService>(1024);

        /// <summary>
        /// 根据名称获取运行时服务实例
        /// </summary>
        /// <param name="name">eg:sys.HelloService</param>
        public async ValueTask<IService> TryGetAsync(string name)
        {
            IService instance;
            if (services.TryGet(name, out instance))
                return instance;

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
                if (!services.TryGet(name, out instance))
                {
                    var asm = new ServiceAssemblyLoader(libPath).LoadServiceAssembly(asmData);
                    var sr = name.Split('.');
                    var type = asm.GetType($"{sr[0]}.ServiceLogic.{sr[1]}", true);
                    instance = (IService)Activator.CreateInstance(type);
                    services.TryAdd(name, instance);
                    Log.Debug($"加载服务实例: {asm.FullName}");
                }
            }
            return instance;
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
            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetExtension(files[i]) == ".dll")
                {
                    var sr = Path.GetFileName(files[i]).Split('.');
                    var asm = new ServiceAssemblyLoader(debugFolder/*TODO:fix path*/).LoadFromAssemblyPath(files[i]);
                    var type = asm.GetType($"{sr[0]}.ServiceLogic.{sr[2]}", true);
                    var instance = (IService)Activator.CreateInstance(type);
                    services.TryAdd($"{sr[0]}.{sr[2]}", instance);
                    Log.Debug("Inject debug service instance:" + files[i]);
                }
            }
        }

        public bool TryRemove(string name)
        {
            return services.TryRemove(name);
        }

    }
}
