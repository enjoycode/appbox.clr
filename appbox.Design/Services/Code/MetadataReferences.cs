using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using appbox.Runtime;
using System.IO.Compression;

namespace appbox.Design
{
    static class MetadataReferences
    {
        // https://github.com/dotnet/roslyn/wiki/Runtime-code-generation-using-Roslyn-compilations-in-.NET-Core-App
        // msbuild /v:detailed
        // xbuild /usr/local/share/dotnet/sdk/NuGetFallbackFolder/netstandard.library/2.0.3/build/netstandard2.0/ref/xxxx.dll

        private static readonly Dictionary<string, MetadataReference> metaRefs;
        internal static readonly string LibPath = Path.Combine(RuntimeContext.Current.AppPath, Server.Consts.LibPath);

        internal static MetadataReference CoreLib => Get("System.Private.CoreLib.dll"); //Get("netstandard/mscorlib.dll");
        internal static MetadataReference NetstandardLib => Get("netstandard.dll"); //Get("netstandard/netstandard.dll");
        //internal static MetadataReference SystemCoreLib => Get("System.Core.dll");
        internal static MetadataReference SystemCollectionsLib => Get("System.Collections.dll");
        internal static MetadataReference SystemLinqLib => Get("System.Linq.dll");
        internal static MetadataReference SystemRuntimLib => Get("System.Runtime.dll"); //Get("netstandard/System.Runtime.dll");
        internal static MetadataReference SystemRuntimExtLib => Get("System.Runtime.Extensions.dll"); //Get("netstandard/System.Runtime.Extensions.dll");
        //internal static MetadataReference SystemBuffersLib => Get("System.Buffers.dll");
        internal static MetadataReference TasksLib => Get("System.Threading.Tasks.dll"); //Get("netstandard/System.Threading.Tasks.dll");
        internal static MetadataReference TasksExtLib => Get("System.Threading.Tasks.Extensions.dll");//Get("netstandard/System.Threading.Tasks.Extensions.dll");
        internal static MetadataReference DataCommonLib => Get("System.Data.Common.dll");
        internal static MetadataReference ComponentModelPrimitivesLib => Get("System.ComponentModel.Primitives.dll");
        //internal static MetadataReference ComponentModelLib => Get("System.ComponentModel.dll");
        internal static MetadataReference JsonLib => Get("System.Text.Json.dll");
        internal static MetadataReference AppBoxCoreLib => Get("appbox.Core.dll");
        internal static MetadataReference AppBoxStoreLib => Get("appbox.Store.dll");

        static MetadataReferences()
        {
            metaRefs = new Dictionary<string, MetadataReference>();
        }

        internal static MetadataReference Get(string asmName, string appName = null)
        {
            // var am =  AssemblyMetadata.CreateFromFile(asmName);
            // var mr = am.GetReference();

            MetadataReference res = null;
            lock (metaRefs)
            {
                if (!metaRefs.TryGetValue(asmName, out res))
                {
                    //先加载内置的组件
                    var path = Path.Combine(LibPath, asmName);
                    res = LoadFromFile(path);
                    if (res != null)
                    {
                        metaRefs.Add(asmName, res);
                        return res;
                    }
                    //再加载外置的组件
                    if (!string.IsNullOrEmpty(appName))
                    {
                        var key = $"{appName}.{asmName}";
                        if (!metaRefs.TryGetValue(key, out res))
                        {
                            res = LoadFromModelStore(key);
                            if (res != null)
                            {
                                metaRefs.Add(key, res);
                                return res;
                            }
                        }
                        else
                        {
                            return res;
                        }
                    }

                    throw new Exception("Cannot get MetadataReference: " + asmName);
                }
            }
            return res;
        }

        private static MetadataReference LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                return MetadataReference.CreateFromFile(filePath);
            }
            catch (Exception ex)
            {
                Log.Warn($"Load MetadataReference[{filePath}] error: {ex.Message}");
                return null;
            }
        }

        private static MetadataReference LoadFromModelStore(string asmName)
        {
            var asmData = Store.ModelStore.LoadServiceAssemblyAsync(asmName).Result;
            //解压缩
            using var oms = new MemoryStream(1024); //TODO：写临时文件?
            using (var ms = new MemoryStream(asmData))
            {
                using var cs = new BrotliStream(ms, CompressionMode.Decompress, true);
                //注意:不支持直接从压缩流中读取
                cs.CopyTo(oms);
            }
            oms.Position = 0;
            try
            {
                return MetadataReference.CreateFromStream(oms);
            }
            catch (Exception ex)
            {
                Log.Warn($"Load MetadataReference[{asmName}] error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 仅用于上传第三方组件后更新相应的缓存
        /// </summary>
        internal static void RemoveMetadataReference(string asmName, string appName)
        {
            if (string.IsNullOrEmpty(appName))
                throw new ArgumentNullException(nameof(appName));

            lock (metaRefs)
            {
                metaRefs.Remove($"{appName}.{asmName}");
            }
        }

    }
}
