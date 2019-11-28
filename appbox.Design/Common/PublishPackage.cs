using System;
using System.Collections.Generic;
using appbox.Models;

namespace appbox.Design
{
    /// <summary>
    /// 用于发布的模型包，支持依赖排序
    /// </summary>
    sealed class PublishPackage //等同于旧实现的PublishModels
    {
        public List<ModelBase> Models { get; }
        public List<ModelFolder> Folders { get; }
        public Dictionary<ulong, byte[]> SourceCodes { get; } //Key=ModelId
        public Dictionary<string, byte[]> ServiceAssemblies { get; } //Value=null表示重命名后需要删除的
        public Dictionary<string, byte[]> ViewAssemblies { get; } //Value=null同上

        public PublishPackage()
        {
            Models = new List<ModelBase>();
            Folders = new List<ModelFolder>();
            SourceCodes = new Dictionary<ulong, byte[]>();
            ServiceAssemblies = new Dictionary<string, byte[]>();
            ViewAssemblies = new Dictionary<string, byte[]>();
        }

        /// <summary>
        /// 根据引用依赖关系排序
        /// </summary>
        public void SortAllModels()
        {
            Models.Sort((a, b) =>
            {
                if (a.ModelType != b.ModelType)
                    return a.ModelType.CompareTo(b.ModelType);

                if (a.ModelType == ModelType.Entity)
                    return ((EntityModel)a).CompareTo((EntityModel)b);

                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }
    }
}
