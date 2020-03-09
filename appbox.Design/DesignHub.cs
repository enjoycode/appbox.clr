using System;
using appbox.Models;
using appbox.Runtime;
using appbox.Serialization;

namespace appbox.Design
{
    /// <summary>
    /// 一个开发者对应一个DesignHub实例
    /// </summary>
    public sealed class DesignHub : IDesignContext, IDisposable //TODO: rename to DesignContext
    {
        #region ====Static Ctor for register serializer====
        static DesignHub()
        {
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.AppPackage, typeof(AppPackage), () => new AppPackage()));
        }
        #endregion

        #region ====Fields & Properties====
        /// <summary>
        /// 当前实例对应的开发者的会话信息
        /// </summary>
        internal ISessionInfo Session { get; private set; }

        /// <summary>
        /// 当前实例对应的设计树实例
        /// </summary>
        public DesignTree DesignTree { get; }

        /// <summary>
        /// 当前实例对应的类型系统，包含Roslyn Workspace信息
        /// </summary>
        internal TypeSystem TypeSystem { get; private set; }

        /// <summary>
        /// 用于发布时暂存挂起的修改
        /// </summary>
        internal object[] PendingChanges { get; set; }

        /// <summary>
        /// 当前设计时的语言
        /// </summary>
        public string CultureName { get; set; } = "zh-CN";

        //private ReportDesignService _reportDesignService;
        ///// <summary>
        ///// 专用于报表设计
        ///// </summary>
        //internal ReportDesignService ReportDesignService
        //{
        //    get
        //    {
        //        if (_reportDesignService == null)
        //        {
        //            lock (this)
        //            {
        //                if (_reportDesignService == null)
        //                    _reportDesignService = new ReportDesignService();
        //            }
        //        }
        //        return _reportDesignService;
        //    }
        //}

        //private WorkflowDesignService _workflowDesignService;
        ///// <summary>
        ///// 专用于工作流设计
        ///// </summary>
        //internal WorkflowDesignService WorkflowDesignService
        //{
        //    get
        //    {
        //        if (_workflowDesignService == null)
        //        {
        //            lock (this)
        //            {
        //                if (_workflowDesignService == null)
        //                    _workflowDesignService = new WorkflowDesignService();
        //            }
        //        }
        //        return _workflowDesignService;
        //    }
        //}

        //private ExpressionDesignService _expressionDesignService;
        ///// <summary>
        ///// 专用于前端表达式编辑器
        ///// </summary>
        //internal ExpressionDesignService ExpressionDesignService
        //{
        //    get
        //    {
        //        if (_expressionDesignService == null)
        //        {
        //            lock (this)
        //            {
        //                if (_expressionDesignService == null)
        //                    _expressionDesignService = new ExpressionDesignService(this);
        //            }
        //        }
        //        return _expressionDesignService;
        //    }
        //}

        private DebugService _debugService;
        /// <summary>
        /// 专用于服务模型调试
        /// </summary>
        internal DebugService DebugService
        {
            get
            {
                if (_debugService == null)
                {
                    lock (this)
                    {
                        if (_debugService == null)
                            _debugService = new DebugService(this);
                    }
                }
                return _debugService;
            }
        }
        #endregion

        #region ====Ctor====
        public DesignHub(ISessionInfo session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            TypeSystem = new TypeSystem();
            DesignTree = new DesignTree(this);
        }
        #endregion

        /// <summary>
        /// 临时方案:用于重新加载设计树时重置TypeSystem
        /// </summary>
        internal void ResetTypeSystem()
        {
            TypeSystem?.Dispose();
            TypeSystem = new TypeSystem();
        }

        #region ====DesignTimeModelContainer====
        public ApplicationModel GetApplicationModel(uint appId)
        {
            return DesignTree.FindApplicationNode(appId).Model;
        }

        public EntityModel GetEntityModel(ulong modelID)
        {
            var modelNode = DesignTree.FindModelNode(ModelType.Entity, modelID);
            if (modelNode != null)
                return (EntityModel)modelNode.Model;
            throw new Exception($"Cannot find EntityModel: {modelID}");
        }

        internal EnumModel GetEnumModel(ulong modelID)
        {
            var modelNode = DesignTree.FindModelNode(ModelType.Enum, modelID);
            if (modelNode != null)
                return (EnumModel)modelNode.Model;
            throw new Exception($"Cannot find EntityModel: {modelID}");
        }

        internal PermissionModel GetPermissionModel(ulong modelID)
        {
            var modelNode = DesignTree.FindModelNode(ModelType.Permission, modelID);
            if (modelNode != null)
                return (PermissionModel)modelNode.Model;
            throw new Exception($"Cannot find EntityModel: {modelID}");
        }
        #endregion

        #region ====IDisposable Support====
        private bool disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_debugService != null)
                    {
                        _debugService.StopDebugger(force: true);
                        _debugService = null;
                    }
                    TypeSystem?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

}
