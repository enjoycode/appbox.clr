using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;

namespace appbox.Server
{

    //参照说明：CoreCLR/Documentation/design-docs/assemblyloadcontext.md

    /// <summary>
    /// 服务实例加载器，支持unload
    /// </summary>
    sealed class ServiceAssemblyLoader : AssemblyLoadContext
    {

        private readonly string libPath;

        public ServiceAssemblyLoader(string libPath) : base(true)
        {
            this.libPath = libPath;
        }

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
            var depFile = Path.Combine(libPath, $"{assemblyName.Name}.dll");
            if (File.Exists(depFile))
            {
                Log.Debug("从文件加载依赖组件: " + assemblyName.FullName);
                //注意：因编译服务模型从流中加载MetadataReference，所以不能用下句加载
                //return LoadFromAssemblyPath(depFile);
                using var fs = File.OpenRead(depFile);
                return LoadFromStream(fs);
            }

            return null; // 返回null意味着所有依赖项程序集都会加载到默认上下文中，当前上下文仅包含显式加载到其中的程序集
            //Log.Debug("加载服务依赖组件: " + assemblyName.FullName);
            //return Default.LoadFromAssemblyName(assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            //TODO:fix 加载第三方原生组件
            Log.Warn($"待实现加载非托管组件: {unmanagedDllName}");
            return base.LoadUnmanagedDll(unmanagedDllName);
        }
    }
}
