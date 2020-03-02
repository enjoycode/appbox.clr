using System;
using System.Threading.Tasks;
using appbox.Models;
using appbox.Store;
using appbox.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace appbox.Design
{
    /// <summary>
    /// 专用于应用模型导入与导出
    /// </summary>
    static class AppStoreService
    {
        /// <summary>
        /// 导出应用模型包
        /// </summary>
        internal static async Task<AppPackage> Export(string appName)
        {
            //判断权限
            if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
                throw new Exception("Must login as a Developer");
            var desighHub = developerSession.GetDesignHub();
            if (desighHub == null)
                throw new Exception("Cannot get DesignContext");

            var appNode = desighHub.DesignTree.FindApplicationNodeByName(appName);
            if (appNode == null)
                throw new Exception($"Can't find application: {appName}");

            //注意目前导出的是最近发布的版本，跟当前设计时版本无关
            var pkg = new AppPackage();
            await ModelStore.LoadToAppPackage(appNode.Model.Id, appName, pkg);
            //TODO:考虑sys包忽略特定模型（即不允许导出）
            return pkg;
        }

        /// <summary>
        /// 导入应用模型包
        /// </summary>
        internal static async Task Import(AppPackage pkg)
        {
            //判断权限
            if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
                throw new Exception("Must login as a Developer");
            var desighHub = developerSession.GetDesignHub();
            if (desighHub == null)
                throw new Exception("Cannot get DesignContext");

            //TODO:先检查导入的实体模型所依赖的相应存储是否存在

            //判断本地有没有相应的App存在
            var localAppNode = desighHub.DesignTree.FindApplicationNode(pkg.Application.Id);
            if (localAppNode == null)
                ImportApp(pkg);
            else
                await UpdateApp(desighHub, pkg, localAppNode.Model);
        }

        private static void ImportApp(AppPackage pkg)
        {
            throw new NotImplementedException();
        }

        private static async Task UpdateApp(DesignHub ctx, AppPackage from, ApplicationModel localAppModel)
        {
            //TODO:考虑删除本地已签出的所有变更
            //TODO:1.签出本地对应App的所有节点，包括模型根节点

            var local = new AppPackage();
            await ModelStore.LoadToAppPackage(localAppModel.Id, localAppModel.Name, local);

            var publish = new PublishPackage();
            //----比对文件夹----
            var folderComparer = new FolderComparer();
            var newFolders = from.Folders.Except(local.Folders, folderComparer);
            foreach (var newFolder in newFolders)
            {
                newFolder.Import();
                publish.Folders.Add(newFolder);
            }
            //var removedFolders = local.Folders.Except(from.Folders, folderComparer);
            //foreach (var removedFolder in removedFolders)
            //{
            //    removedFolder.Remove();
            //    publish.Folders.Add(removedFolder);
            //}
            var otherFolders = local.Folders.Intersect(from.Folders, folderComparer);
            foreach (var folder in otherFolders)
            {
                if (folder.UpdateFrom(from.Folders.Single(t => t.TargetModelType == folder.TargetModelType && t.Id == folder.Id)))
                    publish.Folders.Add(folder);
            }

            //----比对模型----
            var modelComparer = new ModelComparer();
            var newModels = from.Models.Except(local.Models, modelComparer);
            foreach (var newModel in newModels)
            {
                newModel.Import();
                publish.Models.Add(newModel);
                //导入相关代码及Assembly
                if (newModel.ModelType == ModelType.Service)
                {
                    publish.SourceCodes.Add(newModel.Id, from.SourceCodes[newModel.Id]);
                    var key = $"{from.Application.Name}.Services.{newModel.Name}";
                    publish.ServiceAssemblies.Add(key, from.ServiceAssemblies[key]);
                }
                else if (newModel.ModelType == ModelType.View)
                {
                    publish.SourceCodes.Add(newModel.Id, from.SourceCodes[newModel.Id]);
                    var key = $"{from.Application.Name}.Views.{newModel.Name}";
                    publish.ViewAssemblies.Add(key, from.ViewAssemblies[key]);
                }
            }
            //if (localAppModel.ID != SysGlobal.SysString) //注意：系统应用包不移除仅本地有的模型
            //{
            var removedModles = local.Models.Except(from.Models, modelComparer);
            foreach (var removedModel in removedModles)
            {
                removedModel.MarkDeleted();
                publish.Models.Add(removedModel);
                //删除模型的相关代码组件等由PublishService处理，不用再加入
            }
            //}
            var otherModels = local.Models.Intersect(from.Models, modelComparer);
            foreach (var model in otherModels)
            {
                if (model.UpdateFrom(from.Models.Single(t => t.ModelType == model.ModelType && t.Id == model.Id)))
                    publish.Models.Add(model);
                if (model.ModelType == ModelType.Service)
                {
                    publish.SourceCodes.Add(model.Id, from.SourceCodes[model.Id]);
                    var key = $"{from.Application.Name}.Services.{model.Name}";
                    publish.ServiceAssemblies.Add(key, from.ServiceAssemblies[key]);
                }
                else if (model.ModelType == ModelType.View)
                {
                    publish.SourceCodes.Add(model.Id, from.SourceCodes[model.Id]);
                    var key = $"{from.Application.Name}.Views.{model.Name}";
                    publish.ViewAssemblies.Add(key, from.ViewAssemblies[key]);
                }
            }

            //发布变更的包
            await PublishService.PublishAsync(ctx, publish, "Import");
        }

        #region ====EqualityComparer====
        private class FolderComparer : IEqualityComparer<ModelFolder>
        {
            public bool Equals(ModelFolder x, ModelFolder y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.AppId == y.AppId && x.TargetModelType == y.TargetModelType && x.Id == y.Id;
            }

            public int GetHashCode(ModelFolder obj)
            {
                return obj == null ? 0 : obj.Id.GetHashCode() ^ (int)obj.TargetModelType;
            }
        }

        private class ModelComparer : IEqualityComparer<ModelBase>
        {
            public bool Equals(ModelBase x, ModelBase y)
            {
                if (Object.ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.AppId == y.AppId && x.ModelType == y.ModelType && x.Id == y.Id;
            }

            public int GetHashCode(ModelBase obj)
            {
                return obj == null ? 0 : obj.Id.GetHashCode() ^ (int)obj.ModelType;
            }
        }
        #endregion
    }
}
