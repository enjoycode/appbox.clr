using System;
using System.Collections.Generic;
using appbox.Models;

namespace appbox.Design
{
    /// <summary>
    /// 用于发布的模型包，支持依赖排序
    /// </summary>
    class PublishPackage //等同于旧实现的PublishModels
    {
        public List<ModelBase> Models { get; protected set; }
        /// <summary>
        /// 需要保存或删除的模型根文件夹
        /// </summary>
        public List<ModelFolder> Folders { get; protected set; }
        /// <summary>
        /// 新建或更新的模型的虚拟代码，Key=ModelId
        /// </summary>
        public Dictionary<ulong, byte[]> SourceCodes { get; protected set; }
        /// <summary>
        /// 新建或更新的编译好的服务组件, Key=xxx.XXXX
        /// </summary>
        public Dictionary<string, byte[]> ServiceAssemblies { get; protected set; }
        /// <summary>
        /// 新建或更新的视图组件, Key=xxx.XXXX
        /// </summary>
        public Dictionary<string, byte[]> ViewAssemblies { get; protected set; }

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
                //先将标为删除的排在前面
                if (a.PersistentState == Data.PersistentState.Deleted
                        && b.PersistentState != Data.PersistentState.Deleted)
                    return -1;
                if (a.PersistentState != Data.PersistentState.Deleted
                        && b.PersistentState == Data.PersistentState.Deleted)
                    return 1;
                //后面根据类型及依赖关系排序
                if (a.ModelType != b.ModelType)
                    return a.ModelType.CompareTo(b.ModelType);
                if (a.ModelType == ModelType.Entity)
                {
                    //注意如果都标为删除需要倒序
                    if (a.PersistentState == Data.PersistentState.Deleted)
                        return ((EntityModel)b).CompareTo((EntityModel)a);
                    return ((EntityModel)a).CompareTo((EntityModel)b);
                }
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }
    }
}
