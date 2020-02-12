using System;

namespace OmniSharp.Roslyn.CSharp.Services
{
    public sealed class SignatureHelpParameter
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public string Documentation { get; set; }

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("label", Label);
            if (!string.IsNullOrEmpty(Documentation))
            {
                writer.WritePropertyName("documentation");
                //monaco.IMarkdownString
                writer.WriteStartObject();
                writer.WriteString("value", $"**{Name}**: {Documentation}");
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SignatureHelpParameter other))
            {
                return false;
            }

            return Name == other.Name
                && Label == other.Label
                && Documentation == other.Documentation;
        }

        public override int GetHashCode()
        {
            return 17 * Name.GetHashCode()
                + 23 * Label.GetHashCode()
                + 31 * Documentation.GetHashCode();
        }
    }
}
