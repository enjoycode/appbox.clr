using System;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Design
{
    /// <summary>
    /// 调用新建节点后返回的结果
    /// </summary>
    internal struct NewNodeResult : IJsonSerializable
    {

        public int ParentNodeType;
        public string ParentNodeID;
        public DesignNode NewNode;
        /// <summary>
        /// 用于判断模型根节点是否已签出(非自动签出)，用于前端判断是否需要刷新模型根节点
        /// </summary>
        public string RootNodeID;
        /// <summary>
        /// 用于前端处理插入点，由后端排好序后返回给前端，省得前端处理排序问题
        /// </summary>
        public int InsertIndex;

        public string JsonObjID => string.Empty;

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteNumber(nameof(ParentNodeType), ParentNodeType);
            writer.WriteString(nameof(ParentNodeID), ParentNodeID);
            writer.WritePropertyName(nameof(NewNode));
            writer.Serialize(NewNode, objrefs);
            if (!string.IsNullOrEmpty(RootNodeID))
                writer.WriteString(nameof(RootNodeID), RootNodeID);
            writer.WriteNumber(nameof(InsertIndex), InsertIndex);
        }
    }
}
