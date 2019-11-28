using System;
using appbox.Models;
using appbox.Serialization;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace appbox.Design
{
    abstract class Reference : IComparable<Reference>, IJsonSerializable
    {
        public ModelType ModelType { get; private set; }

        public string ModelID { get; private set; }

        public abstract string Location { get; }

        public string JsonObjID => string.Empty;

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        //public abstract String Expression { get; }

        public Reference(ModelType modelType, string modelID)
        {
            ModelType = modelType;
            ModelID = modelID;
        }

        public int CompareTo(Reference other)
        {
            if (ModelType != other.ModelType || ModelID != other.ModelID)
                return string.Compare(ModelID, other.ModelID);

            return CompareSameModel(other);
        }

        public virtual int CompareSameModel(Reference other)
        {
            return 0;
        }

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("Type");
            writer.WriteValue(ModelType.ToString());
            writer.WritePropertyName("Model");
            writer.WriteValue(ModelID);
            writer.WritePropertyName("Location");
            writer.WriteValue(Location);

            WriteMember(writer);
        }

        protected virtual void WriteMember(JsonTextWriter writer)
        { }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
    }

    sealed class CodeReference : Reference
    {

        #region ====Properties====
        public int Offset { get; }

        public int Length { get; }

        public override string Location
        {
            get { return string.Format("[{0} - {1}]", Offset, Length); }
        }

        //private string _expression;
        //public override string Expression
        //{ get { return _expression; } }
        #endregion

        #region ====Ctor====
        public CodeReference(ModelType modelType, string modelID, int offset, int length)
            : base(modelType, modelID)
        {
            Offset = offset;
            Length = length;
            //this._expression = expression;
        }
        #endregion

        #region ====Methods====
        protected override void WriteMember(JsonTextWriter writer)
        {
            writer.WritePropertyName("Offset");
            writer.WriteValue(Offset);
            writer.WritePropertyName("Length");
            writer.WriteValue(Length);
        }

        public override int CompareSameModel(Reference other)
        {
            var r = (CodeReference)other;
            return Offset.CompareTo(r.Offset);
        }

        /// <summary>
        /// 重命名
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="newName"></param>
        internal void Rename(DesignHub hub, ModelNode node, int diff, string newName)
        {
            if (node.NodeType == DesignNodeType.ServiceModelNode) //暂只支持服务模型
            {
                var document = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(node.RoslynDocumentId);
                var sourceText = document.GetTextAsync().Result;
                var startOffset = Offset + diff;
                var endOffset = startOffset + Length;

                sourceText = sourceText.WithChanges(new[] {
                        new TextChange(new TextSpan(startOffset, endOffset - startOffset), newName)
                    });

                hub.TypeSystem.Workspace.OnDocumentChanged(node.RoslynDocumentId, sourceText);
            }
            else
            {
                throw new NotSupportedException("CodeReference is not in ServiceModel");
            }
        }
        #endregion
    }

    sealed class ModelReference : Reference
    {
        public ModelReferenceInfo TargetReference { get; }

        public override string Location
        {
            get { return TargetReference.TargetType.ToString(); }
        }

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
        public ModelReference(ModelType modelType, string modelID, ModelReferenceInfo modelReference)
            : base(modelType, modelID)
        {
            TargetReference = modelReference;
        }
        #endregion

        #region ====Methods====
        protected override void WriteMember(JsonTextWriter writer)
        {
            writer.WritePropertyName("Target");
            writer.WriteValue(TargetReference.Target.ToString());
            writer.WritePropertyName("TargetType");
            writer.WriteValue(TargetReference.TargetType.ToString());
            writer.WritePropertyName("Path");
            writer.WriteValue(TargetReference.Path);
            writer.WritePropertyName("Expression");
            writer.WriteValue(TargetReference.Expression);
        }
        #endregion
    }
}
