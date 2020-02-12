using System.Collections.Generic;

namespace OmniSharp.Models
{
    sealed partial class CodeElement
    {
        public string Kind { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CodeElement> Children { get; }
        public IReadOnlyDictionary<string, Range> Ranges { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

        private CodeElement(
            string kind, string name, string displayName,
            IReadOnlyList<CodeElement> children,
            IReadOnlyDictionary<string, Range> ranges,
            IReadOnlyDictionary<string, object> properties)
        {
            Kind = kind;
            Name = name;
            DisplayName = displayName;
            Children = children;
            Ranges = ranges;
            Properties = properties;
        }

        public override string ToString()
            => $"{Kind} {Name}";

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            //monaco.DocumentSymbol
            writer.WriteStartObject();
            writer.WriteString("name", DisplayName);
            writer.WriteString("detail", "");
            writer.WriteNumber("kind", SymbolKinds.ToMonacoKind(Kind));

            var fullRange = Ranges["full"];
            var nameRange = Ranges["name"];
            writer.WritePropertyName("range");
            fullRange.WriteToJson(writer);
            writer.WritePropertyName("selectionRange");
            nameRange.WriteToJson(writer);

            if(Children != null)
            {
                writer.WritePropertyName("children");
                writer.WriteStartArray();
                foreach (var child in Children)
                {
                    child.WriteToJson(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
