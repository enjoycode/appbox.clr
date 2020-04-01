using System;

namespace appbox.Models
{
    /// <summary>
    /// 模型引用的类型
    /// </summary>
    public enum ModelReferenceType : byte
    {
        ApplicationID,
        EntityModelID,
        EntityMemberName,
        EntityIndexName,
        ServiceModelID,
        ServiceMethodName,
        EnumModelID,
        EnumModelItemName,
        FormModelID,
        ReportModelID,
        WorkflowModelID,
        PermissionModelID,
        ResourceModelID,
        MenuFolderID,
        MenuItemID,
        EventModelID
    }

    /// <summary>
    /// 模型引用的位置
    /// </summary>
    public enum ModelReferencePosition : byte
    {
        LocalizedName,
        EntityModel_ToStringExpression,
        EntityDataFieldModel_EnumModelID,
        EntityFormulaModel_Formula,
        EntityAggregateModel_TargetEntitySet,
        EntityRefModel_RefModelID,
        EntityRefModel_IDMemberName,
        EntityRefModel_TypeMemberName,
        EntitySetModel_RefModelID,
        EntitySetModel_RefMemberName,
        EntitySetModel_RefRowNumberMemberName,
        EntityActionPermissionRule_PermissionModelID,
        EntityActionPermissionRule_Filter,
        EntityUserAction_CanExecuteExpression,
        EntityUserAction_ExecuteExpression,
        EntityDeleteAction_CanExecuteExpression,
        EntityDeleteAction_ExecuteExpression
    }

    /// <summary>
    /// 模型或模型成员名称引用者接口
    /// </summary>
    /// <remarks>
    /// 实现该接口的对象引用了某一模型或其成员的名称，在引用的模型或成员重命名时做相应的重命名操作；
    /// 只要是具备表达式成员的对象或者是引用了其他模型或模型成员名称的对象就必须实现该接口
    /// </remarks>
    public interface IModelReference
    {

        /// <summary>
        /// 重命名引用的模型标识或成员
        /// </summary>
        /// <param name="sourceType">引用的源类型</param>
        /// <param name="targetType">引用的目标类型</param>
        /// <param name="modelID"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        void RenameReference(ModelReferenceType sourceType, ModelReferencePosition targetType,
            ulong modelID, string oldName, string newName);

    }

    public sealed class ModelReferenceInfo
    {

        public IModelReference Target { get; }

        public ModelReferencePosition TargetType { get; }

        public string Path { get; }

        public string Expression { get; }

        public ModelReferenceInfo(IModelReference target,
            ModelReferencePosition targetType, string path, string expression)
        {
            Target = target;
            TargetType = targetType;
            Path = path;
            Expression = expression;
        }

    }
}
