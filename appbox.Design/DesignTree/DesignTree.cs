using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Design
{
    public sealed class DesignTree
    {

        private int _loadFlag; //用于判断是否已加载过整个树
        internal bool HasLoad => System.Threading.Volatile.Read(ref _loadFlag) == 2;

        /// <summary>
        /// 仅用于加载树时临时放入挂起的模型
        /// </summary>
        internal StagedItems Staged { get; private set; }

        internal DesignHub DesignHub { get; private set; }

        public NodeCollection Nodes { get; private set; }
        internal DataStoreRootNode StoreRootNode { get; private set; }
        internal ApplicationRootNode AppRootNode { get; private set; }

        public DesignTree(DesignHub hub)
        {
            DesignHub = hub;
            Nodes = new NodeCollection(null);
        }

        #region ====LoadMethod====
        internal async Task LoadNodesAsync()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _loadFlag, 1, 0) != 0)
                throw new Exception("DesignTree has loaded or loading.");

            StoreRootNode = new DataStoreRootNode(this);
            Nodes.Add(StoreRootNode);
            AppRootNode = new ApplicationRootNode(this);
            Nodes.Add(AppRootNode);

            //先加载签出信息及StagedModels
            _checkouts = await CheckoutService.LoadAllAsync();
            Staged = await StagedService.LoadStagedAsync(onlyModelsAndFolders: true);

            var amodels = await Store.ModelStore.LoadAllApplicationAsync();
            var applicationModels = new List<ApplicationModel>(amodels);
            applicationModels.Sort((a, b) => a.Name.CompareTo(b.Name));

            var mfolders = await Store.ModelStore.LoadAllFolderAsync();
            var folders = new List<ModelFolder>(mfolders);
            //从staged中添加新建的并更新修改的文件夹
            Staged.UpdateFolders(folders);

            var mmodels = await Store.ModelStore.LoadAllModelAsync();
            var models = new List<ModelBase>(mmodels);
#if !FUTURE
            //加载默认存储模型
            var defaultStoreType = Store.SqlStore.Default.GetType();
            var defaultStoreModel = new DataStoreModel(DataStoreKind.Sql,
                $"{defaultStoreType.Assembly.GetName().Name};{defaultStoreType.Name}", "Default");
            defaultStoreModel.NameRules = DataStoreNameRules.AppPrefixForTable;
            //defaultStoreModel.Settings = ""; //TODO:fix settings
            defaultStoreModel.AcceptChanges();
            models.Add(defaultStoreModel);
#endif
            //加载staged中新建的模型，可能包含DataStoreModel
            models.AddRange(Staged.FindNewModels());

            //加入AppModels节点
            foreach (var app in applicationModels)
            {
                AppRootNode.Nodes.Add(new ApplicationNode(this, app));
            }
            //加入Folders
            foreach (var f in folders)
            {
                FindModelRootNode(f.AppId, f.TargetModelType).AddFolder(f);
            }

            //加入Models
            Staged.RemoveDeletedModels(models); //先移除已删除的
            var allModelNodes = new List<ModelNode>(models.Count);
            foreach (var m in models)
            {
                if (m.ModelType == ModelType.DataStore)
                {
                    var dsModel = (DataStoreModel)m;
                    var dsNode = StoreRootNode.AddModel(dsModel, DesignHub);
                    DesignHub.TypeSystem.CreateStoreDocument(dsNode);
                }
                else
                {
                    allModelNodes.Add(FindModelRootNode(m.AppId, m.ModelType).AddModel(m));
                }
            }
            //在所有节点加载完后创建模型对应的RoslynDocument
            foreach (var n in allModelNodes)
            {
                await DesignHub.TypeSystem.CreateModelDocumentAsync(n);
            }

            System.Threading.Interlocked.Exchange(ref _loadFlag, 2);
            //清空Staged
            Staged = null;

            //#if DEBUG
            //            System.Threading.ThreadPool.QueueUserWorkItem((s) =>
            //            {
            //                DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.ModelProjectId);
            //                //DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.SyncSysServiceProjectId);
            //                DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.ServiceBaseProjectId);
            //            });
            //#endif
        }

#if DEBUG
        /// <summary>
        /// 仅用于单元测试
        /// </summary>
        internal async Task LoadForTest(List<ApplicationModel> apps, List<ModelBase> models)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _loadFlag, 1, 0) != 0)
                throw new Exception("DesignTree has loaded or loading.");

            StoreRootNode = new DataStoreRootNode(this);
            Nodes.Add(StoreRootNode);
            AppRootNode = new ApplicationRootNode(this);
            Nodes.Add(AppRootNode);

            _checkouts = new Dictionary<string, CheckoutInfo>();

            //加入AppModels节点
            for (int i = 0; i < apps.Count; i++)
            {
                var appNode = new ApplicationNode(this, apps[i]);
                AppRootNode.Nodes.Add(appNode);
            }
            //加入Models
            var allModelNodes = new ModelNode[models.Count];
            for (int i = 0; i < models.Count; i++)
            {
                allModelNodes[i] = FindModelRootNode(models[i].AppId, models[i].ModelType).AddModel(models[i]);
            }
            //创建RoslynDocument
            for (int i = 0; i < models.Count; i++)
            {
                await DesignHub.TypeSystem.CreateModelDocumentAsync(allModelNodes[i]);
            }

            System.Threading.Interlocked.Exchange(ref _loadFlag, 2);

            DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.ModelProjectId);
            //DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.SyncSysServiceProjectId);
            DesignHub.TypeSystem.DumpProjectErrors(DesignHub.TypeSystem.ServiceBaseProjectId);
        }
#endif
        #endregion

        #region ====Find Methods====
        internal ApplicationNode FindApplicationNodeByName(string appName)
        {
            return (ApplicationNode)AppRootNode.Nodes.Find(n => ((ApplicationNode)n).Model.Name == appName);
        }

        internal ApplicationNode FindApplicationNode(uint appId)
        {
            return (ApplicationNode)AppRootNode.Nodes.Find(n => ((ApplicationNode)n).Model.Id == appId);
        }

        internal DataStoreNode FindDataStoreNodeByName(string name)
        {
            return StoreRootNode.Nodes.Find(t => t.Text == name) as DataStoreNode;
        }

        /// <summary>
        /// 用于前端传回的参数查找对应的设计节点
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        internal DesignNode FindNode(DesignNodeType type, string id)
        {
            switch (type)
            {
                case DesignNodeType.EntityModelNode: return FindModelNode(ModelType.Entity, ulong.Parse(id));
                case DesignNodeType.EnumModelNode: return FindModelNode(ModelType.Enum, ulong.Parse(id));
                case DesignNodeType.ServiceModelNode: return FindModelNode(ModelType.Service, ulong.Parse(id));
                case DesignNodeType.ReportModelNode: return FindModelNode(ModelType.Report, ulong.Parse(id));
                case DesignNodeType.ViewModelNode: return FindModelNode(ModelType.View, ulong.Parse(id));
                case DesignNodeType.ApplicationRoot: return AppRootNode;
                case DesignNodeType.ApplicationNode: return AppRootNode.Nodes.Find(n => n.ID == id);
                case DesignNodeType.ModelRootNode:
                    {
                        var sr = id.Split('-');
                        return FindModelRootNode(uint.Parse(sr[0]), (ModelType)int.Parse(sr[1]));
                    }
                case DesignNodeType.FolderNode: return FindFolderNode(id);
                case DesignNodeType.DataStoreNode: return StoreRootNode.Nodes.Find(t => t.ID == id);
                default: throw ExceptionHelper.NotImplemented(); //todo: fix others
            }
        }

        private FolderNode FindFolderNode(string id)
        {
            Guid folderId = new Guid(id); //注意：id为Guid形式
            for (int i = 0; i < AppRootNode.Nodes.Count; i++)
            {
                var appNode = AppRootNode.Nodes[i] as ApplicationNode;
                if (appNode != null)
                {
                    var folderNode = appNode.FindFolderNode(folderId);
                    if (folderNode != null)
                        return folderNode;
                }
            }

            return null;
        }

        internal ModelRootNode FindModelRootNode(uint appID, ModelType modelType)
        {
            for (int i = 0; i < AppRootNode.Nodes.Count; i++)
            {
                var appNode = (ApplicationNode)AppRootNode.Nodes[i];
                if (appNode.Model.Id == appID)
                {
                    return appNode.FindModelRootNode(modelType);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据模型类型及标识号获取相应的节点
        /// </summary>
        internal ModelNode FindModelNode(ModelType modelType, ulong modelId)
        {
            var appId = IdUtil.GetAppIdFromModelId(modelId);
            var modelRootNode = FindModelRootNode(appId, modelType);
            if (modelRootNode == null)
                return null;

            return modelRootNode.FindModelNode(modelId);
        }

        public ModelNode[] FindNodesByType(ModelType modelType)
        {
            var list = new List<ModelNode>();
            for (int i = 0; i < AppRootNode.Nodes.Count; i++)
            {
                var appNode = (ApplicationNode)AppRootNode.Nodes[i];
                var modelRootNode = appNode.FindModelRootNode(modelType);
                list.AddRange(modelRootNode.GetAllModelNodes());
            }
            return list.ToArray();
        }

        /// <summary>
        /// 查找所有引用指定模型标识的EntityRef Member集合
        /// </summary>
        public List<EntityRefModel> FindEntityRefModels(ulong targetEntityModelID)
        {
            var rs = new List<EntityRefModel>();

            ModelNode[] ls = FindNodesByType(ModelType.Entity);
            for (int i = 0; i < ls.Length; i++)
            {
                EntityModel model = (EntityModel)ls[i].Model;
                //注意：不能排除自身引用，主要指树状结构的实体
                for (int j = 0; j < model.Members.Count; j++)
                {
                    if (model.Members[j].Type == EntityMemberType.EntityRef)
                    {
                        EntityRefModel refMember = (EntityRefModel)model.Members[j];
                        //注意不排除聚合引用
                        for (int k = 0; k < refMember.RefModelIds.Count; k++)
                        {
                            if (refMember.RefModelIds[k] == targetEntityModelID)
                                rs.Add(refMember);
                        }
                    }
                }

            }
            return rs;
        }

        ///// <summary>
        ///// 用于获取所有的AppID
        ///// </summary>
        //internal string[] GetAllAppIDs()
        //{
        //    var res = new string[AppRootNode.Nodes.Count];
        //    for (int i = 0; i < AppRootNode.Nodes.Count; i++)
        //    {
        //        res[i] = ((ApplicationNode)AppRootNode.Nodes[i]).Model.ID;
        //    }
        //    return res;
        //}

        #endregion

        #region ====Find for Create====
        /// <summary>
        /// 用于新建时检查相同名称的模型是否已存在
        /// </summary>
        /// <returns>The model node by name.</returns>
        /// <param name="appId">App identifier.</param>
        /// <param name="type">Type.</param>
        /// <param name="name">Name.</param>
        internal ModelNode FindModelNodeByName(uint appId, ModelType type, string name)
        {
            //TODO:***** 考虑在这里加载存储有没有相同名称的存在,或发布时检测
            // dev1 -> load tree -> checkout -> add model -> publish
            // dev2 -> load tree                                 -> checkout -> add model with same name will pass
            var modelRootNode = FindModelRootNode(appId, type);
            return modelRootNode?.FindModelNodeByName(name);
        }

        /// <summary>
        /// 根据当前选择的节点查询新建模型的上级节点
        /// </summary>
        public DesignNode FindNewModelParentNode(DesignNode selected, out uint appID, ModelType newModelType)
        {
            appID = 0;
            if (selected == null)
                return null;

            DesignNode target = null;
            FindNewModelParentNodeInternal(selected, ref target, ref appID, newModelType);
            return target;
        }

        private static void FindNewModelParentNodeInternal(DesignNode node, ref DesignNode target,
                                                           ref uint appID, ModelType newModelType)
        {
            if (node == null)
                return;

            switch (node.NodeType)
            {
                case DesignNodeType.FolderNode:
                    if (target == null)
                        target = node;
                    break;
                case DesignNodeType.ModelRootNode:
                    ModelRootNode modelRootNode = (ModelRootNode)node;
                    if (newModelType == modelRootNode.TargetType)
                    {
                        if (target == null)
                            target = node;
                        appID = modelRootNode.AppID;
                        return;
                    }
                    break;
                case DesignNodeType.ApplicationNode:
                    target = ((ApplicationNode)node).FindModelRootNode(newModelType);
                    appID = ((ApplicationNode)node).Model.Id;
                    return;
            }

            FindNewModelParentNodeInternal(node.Parent as DesignNode, ref target, ref appID, newModelType);
        }

        /// <summary>
        /// 根据当前选择的节点查找新建文件夹节点的上级节点
        /// </summary>
        public DesignNode FindNewFolderParentNode(DesignNode selected, out uint appID, out ModelType modelType)
        {
            appID = 0;
            modelType = ModelType.Application;

            if (selected == null)
                return null;

            if (selected is ModelRootNode rootNode)
            {
                appID = rootNode.AppID;
                modelType = rootNode.TargetType;
                return selected;
            }
            if (selected.NodeType == DesignNodeType.FolderNode)
            {
                ModelFolder folder = ((FolderNode)selected).Folder;
                appID = folder.AppId;
                modelType = folder.TargetModelType;
                return selected;
            }

            return null;
        }

        /// <summary>
        /// 从上至下查找指定设计节点下的最后一个文件夹的索引号
        /// </summary>
        public int FindLastFolderIndex(DesignNode node)
        {
            if (node.Nodes.Count == 0 || ((DesignNode)node.Nodes[0]).NodeType != DesignNodeType.FolderNode)
                return -1;

            int r = -1;
            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (((DesignNode)node.Nodes[i]).NodeType == DesignNodeType.FolderNode)
                    r = i;
                else
                    return r;
            }

            return r;
        }
        #endregion

        #region ====Checkout Info manager====
        Dictionary<string, CheckoutInfo> _checkouts;

        /// <summary>
        /// 用于签出节点成功后添加签出信息列表
        /// </summary>
        internal void AddCheckoutInfos(List<CheckoutInfo> infos)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                string key = CheckoutInfo.MakeKey(infos[i].NodeType, infos[i].TargetID);
                if (!_checkouts.ContainsKey(key))
                    _checkouts.Add(key, infos[i]);
            }
        }

        /// <summary>
        /// 给设计节点添加签出信息，如果已签出的模型节点则用本地存储替换原模型
        /// </summary>
        internal void BindCheckoutInfo(DesignNode node, bool isNewNode)
        {
            //if (node.NodeType == DesignNodeType.FolderNode || !node.AllowCheckout)
            //    throw new ArgumentException("不允许绑定签出信息: " + node.NodeType.ToString());

            //先判断是否新增的
            if (isNewNode)
            {
                node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, node.Version,
                    DesignHub.Session.Name, DesignHub.Session.LeafOrgUnitID);
                return;
            }

            //非新增的比对服务端的签出列表
            string key = CheckoutInfo.MakeKey(node.NodeType, node.CheckoutInfoTargetID);
            if (_checkouts.TryGetValue(key, out CheckoutInfo checkout))
            {
                node.CheckoutInfo = checkout;
                if (node.IsCheckoutByMe) //如果是被当前用户签出的模型
                {
                    if (node is ModelNode modelNode)
                    {
                        //从本地缓存加载
                        var stagedModel = Staged.FindModel(modelNode.Model.Id);
                        if (stagedModel != null)
                            modelNode.Model = stagedModel;
                    }
                }
            }
        }

        /// <summary>
        /// 部署完后更新所有模型节点的状态，并移除待删除的节点
        /// </summary>
        public void CheckinAllNodes()
        {
            //循环更新模型节点
            for (int i = 0; i < AppRootNode.Nodes.Count; i++)
            {
                ((ApplicationNode)AppRootNode.Nodes[i]).CheckinAllNodes();
            }

            //刷新签出信息表，移除被自己签出的信息
            var list = new List<string>();
            foreach (var key in _checkouts.Keys)
            {
                if (_checkouts[key].DeveloperOuid == RuntimeContext.Current.CurrentSession.LeafOrgUnitID)
                    list.Add(key);
            }
            for (int i = 0; i < list.Count; i++)
            {
                _checkouts.Remove(list[i]);
            }
        }
        #endregion
    }

}
