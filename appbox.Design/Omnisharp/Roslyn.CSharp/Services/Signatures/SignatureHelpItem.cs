using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniSharp.Roslyn.CSharp.Services
{
    public sealed class SignatureHelpItem
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public string Documentation { get; set; }

        public IEnumerable<SignatureHelpParameter> Parameters { get; set; }

        public DocumentationComment StructuredDocumentation { get; set; }

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            //对应monaco.SignatureInformation
            writer.WriteStartObject();
            writer.WriteString("label", Label);
            writer.WriteString("documentation", StructuredDocumentation.SummaryText);
            writer.WritePropertyName("parameters");
            writer.WriteStartArray();
            foreach (var item in Parameters)
            {
                item.WriteToJson(writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SignatureHelpItem other))
            {
                return false;
            }

            return Name == other.Name
                && Label == other.Label
                && Documentation == other.Documentation
                && Enumerable.SequenceEqual(Parameters, other.Parameters);
        }

        public override int GetHashCode()
        {
            return 17 * Name.GetHashCode()
                + 23 * Label.GetHashCode()
                + 31 * Documentation.GetHashCode()
                + Enumerable.Aggregate(Parameters, 37, (current, element) => current + element.GetHashCode());
        }
    }
}
