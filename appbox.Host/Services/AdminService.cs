using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Services
{
    /// <summary>
    /// 系统管理员服务，主要用于权限管理
    /// </summary>
    sealed class AdminService : IService
    {
        /// <summary>
        /// 用于前端组织结构权限管理界面加载整个权限树
        /// </summary>
        public async Task<object> LoadPermissionNodes()
        {
            var list = new List<PermissionNode>();

            var apps = await Store.ModelStore.LoadAllApplicationAsync();
            //TODO:***暂简单实现加载全部，待优化为加载特定类型
            var allFolders = await Store.ModelStore.LoadAllFolderAsync();
            var allModels = await Store.ModelStore.LoadAllModelAsync();
            var folders = allFolders.Where(t => t.TargetModelType == ModelType.Permission);
            var permissions = allModels.Where(t => t.ModelType == ModelType.Permission);

            for (int i = 0; i < apps.Length; i++)
            {
                var appNode = new PermissionNode(apps[i].Name);
                list.Add(appNode);

                var folderIndex = new Dictionary<Guid, PermissionNode>();
                //加载文件夹
                var rootFolder = folders.SingleOrDefault(t => t.AppId == apps[i].Id);
                if (rootFolder != null)
                    LoopAddFolder(folderIndex, appNode, rootFolder);
                //加载PermissionModels
                var appPermissions = permissions.Where(t => t.AppId == apps[i].Id).Cast<PermissionModel>();
                foreach (var p in appPermissions)
                {
                    var modelNode = new PermissionNode(p);
                    if (p.FolderId.HasValue)
                    {
                        if (folderIndex.TryGetValue(p.FolderId.Value, out PermissionNode folderNode))
                            folderNode.Childs.Add(modelNode);
                        else
                            appNode.Childs.Add(modelNode);
                    }
                    else
                    {
                        appNode.Childs.Add(modelNode);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 用于前端实时保存单个PermissionModel的权限变更
        /// </summary>
        public async Task<object> SavePermission(string id, ObjectArray orgunits)
        {
            EnsureIsAdmin();

            var oldModel = (PermissionModel)await Store.ModelStore.LoadModelAsync(ulong.Parse(id));
            if (oldModel == null)
                throw new Exception($"未能找到标识={id}的权限模型");
            //开始重置
            if (oldModel.HasOrgUnits)
                oldModel.OrgUnits.Clear();
            if (orgunits != null)
            {
                for (int i = 0; i < orgunits.Count; i++)
                {
                    oldModel.OrgUnits.Add(Guid.Parse((string)orgunits[i]));
                }
            }
            //保存
            //oldModel.InDesign = true;
#if FUTURE
            var txn = await Store.Transaction.BeginAsync();
            await Store.ModelStore.UpdateModelAsync(oldModel, txn, appid => RuntimeContext.Current.GetApplicationModelAsync(appid).Result);
            await txn.CommitAsync();
#else
            using var conn = await Store.SqlStore.Default.OpenConnectionAsync();
            using var txn = conn.BeginTransaction();
            await Store.ModelStore.UpdateModelAsync(oldModel, txn, appid => RuntimeContext.Current.GetApplicationModelAsync(appid).Result);
            txn.Commit();
#endif

            //更新服务端缓存
            RuntimeContext.Current.InvalidModelsCache(null, new ulong[] { oldModel.Id }, true);
            //TODO: 激发模型变更事件
            // string eventArg = string.Format("{0}:{1}", ModelType.Permission, oldModel.OriginalID);
            return null;
        }

        private static void EnsureIsAdmin()
        {
            if (!RuntimeContext.HasPermission(Consts.SYS_PERMISSION_ADMIN_ID))
                throw new Exception("不具备管理员权限");
        }

        private static void LoopAddFolder(Dictionary<Guid, PermissionNode> dic, PermissionNode parent, ModelFolder folder)
        {
            var parentNode = parent;
            if (folder.Parent != null)
            {
                var node = new PermissionNode(folder.Name);
                dic.Add(folder.Id, node);
                parent.Childs.Add(node);
                parentNode = node;
            }

            if (folder.HasChilds)
            {
                for (int i = 0; i < folder.Childs.Count; i++)
                {
                    LoopAddFolder(dic, parentNode, folder.Childs[i]);
                }
            }
        }

        public async ValueTask<AnyValue> InvokeAsync(ReadOnlyMemory<char> method, InvokeArgs args)
        {
            switch (method)
            {
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(LoadPermissionNodes)):
                    return AnyValue.From(await LoadPermissionNodes());
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(SavePermission)):
                    return AnyValue.From(await SavePermission(args.GetString(), args.GetObjectArray()));
                default:
                    throw new Exception($"Can't find method: {method}");
            }
        }
    }
}
