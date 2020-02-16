using System;
using System.Text.Json;
using appbox.Serialization;

namespace appbox.Design
{
    struct TypeScriptDeclare : IJsonSerializable
    {
        public string Name;
        public string Declare;

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString(nameof(Name), Name);
            writer.WriteString(nameof(Declare), Declare);
        }
    }
}
