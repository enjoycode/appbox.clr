using System;
using appbox.Serialization;
using Newtonsoft.Json;


namespace OmniSharp.Roslyn
{

    public struct LinePositionSpanTextChange: IJsonSerializable
    {
        public string NewText { get; set; }

        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }

        // string IJsonSerializable.JsonObjID => string.Empty;

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }

        void IJsonSerializable.WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            //注意: 直接转换为前端monaco-editor需要的格式, 另所有需要+1
            // https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.textedit.html
            writer.WritePropertyName("range");
            writer.WriteStartObject();
            writer.WritePropertyName("startColumn");
            writer.WriteValue(this.StartColumn + 1);
            writer.WritePropertyName("startLineNumber");
            writer.WriteValue(this.StartLine + 1);
            writer.WritePropertyName("endColumn");
            writer.WriteValue(this.EndColumn + 1);
            writer.WritePropertyName("endLineNumber");
            writer.WriteValue(this.EndLine + 1);
            writer.WriteEndObject();

            writer.WritePropertyName("text");
            writer.WriteValue(this.NewText);
        }
    }

}