using System;
using OmniSharp.Mef;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using appbox.Design;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using appbox;

namespace OmniSharp.Roslyn.CSharp.Services
{

    internal sealed class CheckCode : IRequestHandler
    {

        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            int type = args.GetInt32();
            string modelId = args.GetString();

            if (type == 1)
            {
                var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelId));
                if (modelNode == null)
                    throw new Exception($"Cannot find ServiceModel: {modelId}");

                var quickFixes = new List<QuickFix>();

                var document = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(modelNode.RoslynDocumentId);
                var semanticModel = document.GetSemanticModelAsync().Result;
                IEnumerable<Diagnostic> diagnostics = semanticModel.GetDiagnostics();

                return Task.FromResult<object>(diagnostics.Select(MakeQuickFix).ToArray());
            }
            else
            {
                throw ExceptionHelper.NotImplemented();
            }
        }

        private static QuickFix MakeQuickFix(Diagnostic diagnostic)
        {
            var span = diagnostic.Location.GetMappedLineSpan();
            return new QuickFix// DiagnosticLocation
            {
                // FileName = span.Path,
                Line = span.StartLinePosition.Line,
                Column = span.StartLinePosition.Character,
                EndLine = span.EndLinePosition.Line,
                EndColumn = span.EndLinePosition.Character,
                Text = diagnostic.GetMessage(),
                Level = (int)diagnostic.Severity
            };
        }

    }

    internal struct QuickFix : IJsonSerializable
    {
        // public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public int Level { get; set; }
        public string Text { get; set; }

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }

        void IJsonSerializable.WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            //注意：都+1，以方便前端定位
            writer.WritePropertyName(nameof(Line));
            writer.WriteValue(Line + 1);
            writer.WritePropertyName(nameof(Column));
            writer.WriteValue(Column + 1);
            writer.WritePropertyName(nameof(EndLine));
            writer.WriteValue(EndLine + 1);
            writer.WritePropertyName(nameof(EndColumn));
            writer.WriteValue(EndColumn + 1);
            writer.WritePropertyName(nameof(Level));
            writer.WriteValue(Level);
            writer.WritePropertyName(nameof(Text));
            writer.WriteValue(Text);
        }
    }

}