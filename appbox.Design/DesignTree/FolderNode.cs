using System;
using System.Threading.Tasks;
using appbox.Models;

namespace appbox.Design
{
    public sealed class FolderNode : DesignNode
    {
        public override DesignNodeType NodeType => DesignNodeType.FolderNode;

        public ModelFolder Folder { get; internal set; }

        internal override int SortNo => int.MinValue;

        public override string ID => Folder.Id.ToString();

        //注意：处理顶级Folder.Version
        public override uint Version
        {
            get { return Folder.GetRoot().Version; }
            set { Folder.GetRoot().Version = value; }
        }

        //public override CheckoutInfo CheckoutInfo //todo: 移除
        //{
        //    get
        //    {
        //        //注意：返回相应的模型根节点的签出
        //        var rootNode = this.TreeView.FindModelRootNode(this.Folder.AppID, this.Folder.TargetModelType);
        //        return rootNode.CheckoutInfo;
        //    }
        //    set { throw new NotSupportedException("FolderNode can not set CheckoutInfo"); }
        //}

        //public override bool IsCheckoutByMe //todo:移除
        //{
        //    get
        //    {
        //        //注意：返回相应的模型根节点
        //        var rootNode = this.TreeView.FindModelRootNode(this.Folder.AppID, this.Folder.TargetModelType);
        //        return rootNode.IsCheckoutByMe;
        //    }
        //}

        public FolderNode(ModelFolder folder)
        {
            Folder = folder;
            Text = Folder.Name;
        }

        /// <summary>
        /// 移除新建的根目录所保存的本地缓存
        /// </summary>
        internal static void RemoveRootFolderCache(ModelFolder folder)
        {
            throw ExceptionHelper.NotImplemented();
            // string path = PathService.GetFolderPath(folder, true);
            // FileCacheService.DeleteCache(path);
        }

        /// <summary>
        /// 检查指定目录是否原本是根目录
        /// </summary>
        internal static bool CheckRootFolderCacheExists(ModelFolder folder)
        {
            throw ExceptionHelper.NotImplemented();
            // string path = PathService.GetFolderPath(folder, false);
            // ModelFolder current = FileCacheService.LoadFromCache(path) as ModelFolder;
            // if (current == null || current.PersistentState != PersistentState.Deleted)
            //     return false;
            // return true;
        }

        internal Task SaveAsync()
        {
            //查找文件夹直至根级文件夹，然后序列化保存根级文件夹
            ModelFolder rootFolder = Folder.GetRoot();
            //保存节点模型
            return StagedService.SaveFolderAsync(rootFolder);
        }

    }

}
