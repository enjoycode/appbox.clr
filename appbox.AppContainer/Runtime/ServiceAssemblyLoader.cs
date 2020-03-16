using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;

namespace appbox.Server
{

    //参照说明：CoreCLR/Documentation/design-docs/assemblyloadcontext.md
    //TODO: 等.net core 3.0时实现unload

    sealed class ServiceAssemblyLoader : AssemblyLoadContext
    {

        /// <summary>
        /// 辅助方法，方便加载服务模型的Assembly
        /// </summary>
        /// <param name="asmData">压缩过的</param>
        public Assembly LoadServiceAssembly(byte[] asmData)
        {
            if (asmData == null)
                throw new ArgumentNullException(nameof(asmData));

            using var oms = new MemoryStream(1024); //TODO：写临时文件?
            using (var ms = new MemoryStream(asmData))
            {
                using var cs = new BrotliStream(ms, CompressionMode.Decompress, true);
                //注意:不支持直接从压缩流中读取
                cs.CopyTo(oms);
            }
            oms.Position = 0;
            return LoadFromStream(oms);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            //// Console.WriteLine("开始加载: " + assemblyName.FullName);

            //// var deps = DependencyContext.Default;
            //// var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            //// if (res.Count > 0)
            //// {
            ////     Log.Debug("从DependencyContext加载组件:" + assemblyName.FullName);
            ////     return Assembly.Load(new AssemblyName(res.First().Name));
            //// }
            //// else
            //// {
            //var depFile = $"{folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll";
            //if (File.Exists(depFile))
            //{
            //    Log.Debug("从文件加载依赖组件: " + assemblyName.FullName);
            //    return this.LoadFromAssemblyPath(depFile);
            //}
            //// }
            //Log.Debug("加载服务依赖组件: " + assemblyName.FullName);
            return Default.LoadFromAssemblyName(assemblyName);
        }
    }
}
