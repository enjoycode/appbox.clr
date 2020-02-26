using System;
using System.Threading.Tasks;
using appbox.Models;
using appbox.Store;
using appbox.Runtime;

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
    }
}
