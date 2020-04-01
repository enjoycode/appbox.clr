using System;
using appbox.Models;
using appbox.Serialization;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json;

namespace appbox.Design
{
    /// <summary>
    /// 模型或其成员的引用者的基类，目前分为两类：
    /// 1. 模型对模型的引用
    /// 2. 模型内虚拟代码对模型的引用
    /// </summary>
    abstract class Reference : IComparable<Reference>, IJsonSerializable
    {
        public ModelNode ModelNode { get; private set; }

        /// <summary>
        /// 用于友好显示的位置信息
        /// </summary>
        public abstract string Location { get; }

        //public abstract String Expression { get; }

        public Reference(ModelNode modelNode)
        {
            ModelNode = modelNode ?? throw new ArgumentNullException();
        }

        public int CompareTo(Reference other)
        {
            if (ModelNode.Model.ModelType != other.ModelNode.Model.ModelType
                || ModelNode.Model.Id != other.ModelNode.Model.Id)
                return ModelNode.Model.Id.CompareTo(other.ModelNode.Model.Id);

            return CompareSameModel(other);
        }

        public virtual int CompareSameModel(Reference other)
        {
            return 0;
        }

        public override string ToString()
        {
            return $"{ModelNode.AppNode.Model.Name}.{CodeHelper.GetPluralStringOfModelType(ModelNode.Model.ModelType)}.{ModelNode.Model.Name} at {Location}";
        }

        #region ====Serialization====
        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString("Type", ModelNode.Model.ModelType.ToString());
            writer.WriteString("Model", $"{ModelNode.AppNode.Model.Name}.{ModelNode.Model.Name}");
            writer.WriteString("Location", Location);

            WriteMember(writer);
        }

        protected virtual void WriteMember(Utf8JsonWriter writer) { }

        public void ReadFromJson(ref Utf8JsonReader reader,
            ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion
    }

    /// <summary>
    /// 模型内虚拟代码的引用
    /// </summary>
    sealed class CodeReference : Reference
    {

        #region ====Properties====
        public int Offset { get; }

        public int Length { get; }

        public override string Location => $"[{Offset} - {Length}]";

        //private string _expression;
        //public override string Expression
        //{ get { return _expression; } }
        #endregion

        #region ====Ctor====
        public CodeReference(ModelNode modelNode, int offset, int length) : base(modelNode)
        {
            Offset = offset;
            Length = length;
            //this._expression = expression;
        }
        #endregion

        #region ====Methods====
        protected override void WriteMember(Utf8JsonWriter writer)
        {
            writer.WriteNumber("Offset", Offset);
            writer.WriteNumber("Length", Length);
        }

        public override int CompareSameModel(Reference other)
        {
            var r = (CodeReference)other;
            //Log.Warn($"Compare CodeReference: {Offset} {r.Offset}");
            return Offset.CompareTo(r.Offset);
        }

        /// <summary>
        /// 重命名
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="newName"></param>
        internal void Rename(DesignHub hub, int diff, string newName)
        {
            if (ModelNode.NodeType == DesignNodeType.ServiceModelNode) //暂只支持服务模型
            {
                var document = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(ModelNode.RoslynDocumentId);
                var sourceText = document.GetTextAsync().Result;
                var startOffset = Offset + diff;
                var endOffset = startOffset + Length;

                sourceText = sourceText.WithChanges(new[] {
                        new TextChange(new TextSpan(startOffset, endOffset - startOffset), newName)
                    });

                hub.TypeSystem.Workspace.OnDocumentChanged(ModelNode.RoslynDocumentId, sourceText);
            }
            else
            {
                throw new NotSupportedException("CodeReference is not from ServiceModel");
            }
        }
        #endregion
    }

    /// <summary>
    /// 模型对模型的引用，如表达式引用
    /// </summary>
    sealed class ModelReference : Reference
    {
        public ModelReferenceInfo TargetReference { get; }

        public override string Location => TargetReference.TargetType.ToString();

        //public override string Expression
        //{
        //    get
        //    {
        //        if (_modelReference == null)
        //            return null;
        //        return this._modelReference.Expression;
        //    }
        //}

        #region ====Ctor====
        public ModelReference(ModelNode modelNode, ModelReferenceInfo modelReference)
            : base(modelNode)
        {
            TargetReference = modelReference;
        }
        #endregion

        #region ====Methods====
        protected override void WriteMember(Utf8JsonWriter writer)
        {
            writer.WriteString("Target", TargetReference.Target.ToString());
            writer.WriteString("TargetType", TargetReference.TargetType.ToString());
            writer.WriteString("Path", TargetReference.Path);
            writer.WriteString("Expression", TargetReference.Expression);
        }
        #endregion
    }
}
