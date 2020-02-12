using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using System.Text.Json;
using appbox.Serialization;
using appbox.Design;
using appbox.Data;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OmniSharp.Roslyn.CSharp.Services
{
    /// <summary>
    /// The signature help provider interface defines the contract between extensions and the parameter hints-feature.
    /// </summary>
    sealed class SignatureHelp : IRequestHandler
    {
        public sealed class SignatureHelpResponse : IJsonSerializable
        {
            public IEnumerable<SignatureHelpItem> Signatures { get; set; }

            public int ActiveSignature { get; set; }

            public int ActiveParameter { get; set; }

            public PayloadType JsonPayloadType => PayloadType.UnknownType;

            public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

            public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
            {
                writer.WriteNumber("activeSignature", ActiveSignature);
                writer.WriteNumber("activeParameter", ActiveParameter);
                writer.WritePropertyName("signatures");
                writer.WriteStartArray();
                foreach (var item in Signatures)
                {
                    item.WriteToJson(writer);
                }
                writer.WriteEndArray();
            }
        }

        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string fileName = args.GetString();
            int line = args.GetInt32() - 1; //注意：前端传过来的值需要-1
            int column = args.GetInt32() - 1;

            var document = hub.TypeSystem.Workspace.GetOpenedDocumentByName(fileName);
            if (document == null)
                throw new Exception($"Cannot find opened document: {fileName}");

            var invocation = await GetInvocation(document, line, column);
            if (invocation == null)
                return null;

            var response = new SignatureHelpResponse();

            // define active parameter by position
            foreach (var comma in invocation.Separators)
            {
                if (comma.Span.Start > invocation.Position)
                {
                    break;
                }
                response.ActiveParameter += 1;
            }

            // process all signatures, define active signature by types
            var signaturesSet = new HashSet<SignatureHelpItem>();
            var bestScore = int.MinValue;
            SignatureHelpItem bestScoredItem = null;

            var types = invocation.ArgumentTypes;
            ISymbol throughSymbol = null;
            ISymbol throughType = null;
            var methodGroup = invocation.SemanticModel.GetMemberGroup(invocation.Receiver).OfType<IMethodSymbol>();
            if (invocation.Receiver is MemberAccessExpressionSyntax)
            {
                var throughExpression = ((MemberAccessExpressionSyntax)invocation.Receiver).Expression;
                throughSymbol = invocation.SemanticModel.GetSpeculativeSymbolInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsExpression).Symbol;
                throughType = invocation.SemanticModel.GetSpeculativeTypeInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                var includeInstance = (throughSymbol != null && !(throughSymbol is ITypeSymbol)) ||
                    throughExpression is LiteralExpressionSyntax ||
                    throughExpression is TypeOfExpressionSyntax;
                var includeStatic = (throughSymbol is INamedTypeSymbol) || throughType != null;
                methodGroup = methodGroup.Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
            }
            else if (invocation.Receiver is SimpleNameSyntax && invocation.IsInStaticContext)
            {
                methodGroup = methodGroup.Where(m => m.IsStatic || m.MethodKind == MethodKind.LocalFunction);
            }

            foreach (var methodOverload in methodGroup)
            {
                var signature = BuildSignature(methodOverload);
                signaturesSet.Add(signature);

                var score = InvocationScore(methodOverload, types);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoredItem = signature;
                }
            }

            var signaturesList = signaturesSet.ToList();
            response.Signatures = signaturesList;
            response.ActiveSignature = signaturesList.IndexOf(bestScoredItem);

            return response;
        }

        private async Task<InvocationContext> GetInvocation(Document document, int line, int column)
        {
            var sourceText = await document.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(line, column));
            var tree = await document.GetSyntaxTreeAsync();
            var root = await tree.GetRootAsync();
            var node = root.FindToken(position).Parent;

            // Walk up until we find a node that we're interested in.
            while (node != null)
            {
                if (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, invocation.Expression, invocation.ArgumentList, invocation.IsInStaticContext());
                }

                if (node is ObjectCreationExpressionSyntax objectCreation && objectCreation.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, objectCreation, objectCreation.ArgumentList, objectCreation.IsInStaticContext());
                }

                if (node is AttributeSyntax attributeSyntax && attributeSyntax.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, attributeSyntax, attributeSyntax.ArgumentList, attributeSyntax.IsInStaticContext());
                }

                node = node.Parent;
            }

            return null;
        }

        private int InvocationScore(IMethodSymbol symbol, IEnumerable<TypeInfo> types)
        {
            var parameters = symbol.Parameters;
            if (parameters.Count() < types.Count())
            {
                return int.MinValue;
            }

            var score = 0;
            var invocationEnum = types.GetEnumerator();
            var definitionEnum = parameters.GetEnumerator();
            while (invocationEnum.MoveNext() && definitionEnum.MoveNext())
            {
                if (invocationEnum.Current.ConvertedType == null)
                {
                    // 1 point for having a parameter
                    score += 1;
                }
                else if (SymbolEqualityComparer.Default.Equals(invocationEnum.Current.ConvertedType, definitionEnum.Current.Type))
                {
                    // 2 points for having a parameter and being
                    // the same type
                    score += 2;
                }
            }

            return score;
        }

        private static SignatureHelpItem BuildSignature(IMethodSymbol symbol)
        {
            var signature = new SignatureHelpItem();
            signature.Documentation = symbol.GetDocumentationCommentXml();
            signature.Name = symbol.MethodKind == MethodKind.Constructor ? symbol.ContainingType.Name : symbol.Name;
            signature.Label = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            signature.StructuredDocumentation = DocumentationConverter.GetStructuredDocumentation(symbol);

            signature.Parameters = symbol.Parameters.Select(parameter =>
            {
                return new SignatureHelpParameter()
                {
                    Name = parameter.Name,
                    Label = parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    Documentation = signature.StructuredDocumentation.GetParameterText(parameter.Name)
                };
            });

            return signature;
        }
    }
}
