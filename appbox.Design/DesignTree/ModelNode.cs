using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using Microsoft.CodeAnalysis;
using System.Text.Json;

namespace appbox.Design
{

    public sealed class ModelNode : DesignNode
    {
        #region ====Fields & Properties====
        private readonly DesignNodeType nodeType;
        public override DesignNodeType NodeType => nodeType;

        public override string ID => Model.Id.ToString();

        //internal override int SortNo => Model is ISortableDesignObject ? ((ISortableDesignObject)Model).SortNo : base.SortNo;

        internal DocumentId RoslynDocumentId { get; private set; }
        internal ApplicationNode AppNode { get; private set; } //缓存用
        internal ModelBase Model { get; set; }

        public override string CheckoutInfoTargetID => Model.Id.ToString();

        public override uint Version
        {
            get { return Model.Version; }
            set { throw new InvalidOperationException(); }
        }

        #region ----服务模型节点专用----
        internal ProjectId ServiceProjectId { get; private set; }
        //internal DocumentId SyncProxyDocumentId { get; private set; }
        internal DocumentId AsyncProxyDocumentId { get; private set; }
        #endregion
        #endregion

        #region ====Ctor====
        public ModelNode(ModelBase targetModel, DesignHub hub) //注意：新建时尚未加入树，无法获取TreeView实例
        {
            AppNode = hub.DesignTree.FindApplicationNode(targetModel.AppId);
            Model = targetModel;
            Text = targetModel.Name;

            switch (targetModel.ModelType)
            {
                case ModelType.Entity:
                    RoslynDocumentId = DocumentId.CreateNewId(hub.TypeSystem.ModelProjectId);
                    nodeType = DesignNodeType.EntityModelNode;
                    break;
                case ModelType.Service:
                    ServiceProjectId = ProjectId.CreateNewId();
                    RoslynDocumentId = DocumentId.CreateNewId(ServiceProjectId);
                    nodeType = DesignNodeType.ServiceModelNode;

                    //SyncProxyDocumentId = DocumentId.CreateNewId(hub.TypeSystem.SyncServiceProxyProjectId);
                    AsyncProxyDocumentId = DocumentId.CreateNewId(hub.TypeSystem.AsyncServiceProxyProjectId);
                    break;
                case ModelType.View:
                    nodeType = DesignNodeType.ViewModelNode;
                    break;
                case ModelType.Report:
                    nodeType = DesignNodeType.ReportModelNode;
                    break;
                case ModelType.Enum:
                    RoslynDocumentId = DocumentId.CreateNewId(hub.TypeSystem.ModelProjectId);
                    nodeType = DesignNodeType.EnumModelNode;
                    break;
                case ModelType.Event:
                    nodeType = DesignNodeType.EventModelNode;
                    break;
                case ModelType.Permission:
                    RoslynDocumentId = DocumentId.CreateNewId(hub.TypeSystem.ModelProjectId);
                    nodeType = DesignNodeType.PermissionModelNode;
                    break;
                case ModelType.Workflow:
                    RoslynDocumentId = DocumentId.CreateNewId(hub.TypeSystem.WorkflowModelProjectId);
                    nodeType = DesignNodeType.WorkflowModelNode;
                    break;
            }
        }
        #endregion

        #region ====Methods====
        /// <summary>
        /// 保存模型节点及相关资源至本地文件
        /// </summary>
        internal async Task SaveAsync(object[] modelInfos)
        {
            if (!IsCheckoutByMe)
                throw new Exception("ModelNode has not checkout");

            //TODO: 更新相关模型的内容，另考虑事务保存模型及相关代码
            if (Model.PersistentState != PersistentState.Deleted)
            {
                switch (Model.ModelType)
                {
                    case ModelType.Service:
                        {
                            //TODO:*****更新服务模型代理类
                            // if (!this._document.ParsedDocument.HasErrors)
                            // {
                            //     TypeSystemService.UpdateServiceProxyDocument(this.TargetNode.RoslynDocumentId,
                            //                                              this.TargetNode.SyncProxyDocumentId,
                            //                                              this.TargetNode.AsyncProxyDocumentId,
                            //                                              this.TargetModel);
                            // }

                            //保存初始化或更改过的代码
                            string sourceCode;
                            if (modelInfos != null && modelInfos.Length == 1)
                            {
                                sourceCode = (string)modelInfos[0];
                            }
                            else
                            {
                                var doc = DesignTree.DesignHub.TypeSystem.Workspace.CurrentSolution.GetDocument(RoslynDocumentId);
                                var sourceText = await doc.GetTextAsync();
                                sourceCode = sourceText.ToString();
                            }

                            await StagedService.SaveServiceCodeAsync(Model.Id, sourceCode);
                        }
                        break;
                    case ModelType.View:
                        {
                            if (modelInfos != null)
                            {
                                await StagedService.SaveViewCodeAsync(Model.Id, (string)modelInfos[0], (string)modelInfos[1], (string)modelInfos[2]);
                                await StagedService.SaveViewRuntimeCodeAsync(Model.Id, (string)modelInfos[3]);
                            }
                        }
                        break;
                        //case ModelType.Report:
                        //this.TreeView.DesignHub.ReportDesignService.FlushReportDefinition(this);
                        //break;
                }

                //注意：不再在此更新RoslynDocument, 实体模型通过设计命令更新,服务模型通过前端代码编辑器实时更新
            }

            //保存节点模型
            await StagedService.SaveModelAsync(Model);
        }
        #endregion

        #region ====Serialization====
        public override void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteNumber("SortNo", SortNo);
            writer.WriteString("App", AppNode.Model.Name);
            writer.WriteString("Name", Model.Name);
            writer.WriteNumber("ModelType", (int)Model.ModelType);
            if (Model.ModelType == ModelType.Entity)
            {
                var entityModel = (EntityModel)Model;
                //writer.WritePropertyName("LocalizedName");
                //writer.WriteValue(entityModel.LocalizedName.Value);

                //EntityModel输出对应的存储标识，方便前端IDE筛选相同存储的实体
                if (entityModel.SysStoreOptions != null)
                {
                    writer.WriteNumber("StoreId", 0);
                }
                else if (entityModel.SqlStoreOptions != null)
                {
                    writer.WriteNumber("StoreId", entityModel.SqlStoreOptions.StoreModelId);
                }
            }
        }
        #endregion
    }

}
