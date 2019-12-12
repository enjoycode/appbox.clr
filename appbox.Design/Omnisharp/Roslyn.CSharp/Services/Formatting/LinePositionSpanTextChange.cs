using System;
using appbox.Serialization;
using System.Text.Json;

namespace OmniSharp.Roslyn
{

    public struct LinePositionSpanTextChange : IJsonSerializable
    {
        public string NewText { get; set; }

        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }

        // string IJsonSerializable.JsonObjID => string.Empty;

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

        void IJsonSerializable.WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            //注意: 直接转换为前端monaco-editor需要的格式, 另所有需要+1
            // https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.textedit.html
            writer.WritePropertyName("range");
            writer.WriteStartObject();
            writer.WriteNumber("startColumn", StartColumn + 1);
            writer.WriteNumber("startLineNumber", StartLine + 1);
            writer.WriteNumber("endColumn", EndColumn + 1);
            writer.WriteNumber("endLineNumber", EndLine + 1);
            writer.WriteEndObject();

            writer.WriteString("text", NewText);
        }
    }

}