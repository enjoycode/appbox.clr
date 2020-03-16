using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 保存上传的第三方组件
    /// </summary>
    sealed class Upload3rdLib : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string fileName = args.GetString();
            string tempFile = args.GetString();
            string appName = args.GetString();

            if (string.IsNullOrEmpty(appName))
                throw new ArgumentException("必须指明App");

            //找到对应的ApplicationNode
            var appNode = hub.DesignTree.FindApplicationNodeByName(appName);
            if (appNode == null)
                throw new Exception("未能找到应用节点: " + appName);

            //判断组件类型
            AssemblyPlatform platform = AssemblyPlatform.Common;
            var ext = Path.GetExtension(fileName);
            if (ext == "so")
                platform = AssemblyPlatform.Linux;
            else if (ext == "dylib")
                platform = AssemblyPlatform.OSX;
            else if (!IsDotNetAssembly(tempFile))
                platform = AssemblyPlatform.Windows;

            //压缩组件
            using var dllStream = new MemoryStream(1024);
            using var cs = new BrotliStream(dllStream, CompressionMode.Compress, true);
            using var fs = File.OpenRead(tempFile);
            await fs.CopyToAsync(cs);
            await cs.FlushAsync();
            var asmData = dllStream.ToArray();

            //保存组件
            var asmName = $"{appName}.{fileName}";
            await Store.ModelStore.UpsertAssemblyAsync(asmName, asmData);

            //TODO:*****
            // 1. 通知所有DesignHub.TypeSystem更新MetadataReference缓存，并更新相关项目引用
            //TypeSystem.RemoveMetadataReference(fileName, appID);
            // 2. 如果相应的AppContainer已启动，通知其移除所有引用该第三方组件的服务实例缓存，使其自动重新加载
            // 3. 如果集群模式，通知集群其他节点做上述1，2操作

            return platform == AssemblyPlatform.Common ? true : false; //返回true表示.net 组件可引用, false表示native不可直接引用
        }

        // https://stackoverflow.com/questions/1366503/best-way-to-check-if-a-dll-file-is-a-clr-assembly-in-c-sharp
        // http://msdn.microsoft.com/en-us/library/ms173100.aspx
        private static bool IsDotNetAssembly(string file)
        {
            try
            {
                var asm = System.Reflection.AssemblyName.GetAssemblyName(file);
                //TODO: 考虑验证运行时框架
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
