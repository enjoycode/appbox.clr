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
// using OmniSharp.Options;
// using OmniSharp.Roslyn.CSharp.Services.Documentation;
using System.Text.Json;
using appbox.Serialization;
using appbox.Design;
using appbox.Data;

namespace OmniSharp.Roslyn.CSharp.Services
{

    internal static class KindHelper
    {
        private static readonly Dictionary<string, int> _kinds = new Dictionary<string, int>();

        static KindHelper()
        {
            // types
            _kinds["Class"] = 6;
            _kinds["Delegate"] = 6; // need a better option for this.
            _kinds["Enum"] = 12;
            _kinds["Interface"] = 7;
            _kinds["Struct"] = 21;//CompletionItemKind.Struct;

            // variables
            _kinds["Local"] = 5;
            _kinds["Parameter"] = 5;
            _kinds["RangeVariable"] = 5;

            // members
            _kinds["Const"] = 20;
            _kinds["EnumMember"] = 19;
            _kinds["Event"] = 22;
            _kinds["Field"] = 4;
            _kinds["Method"] = 1;
            _kinds["Property"] = 9;

            // other stuff
            _kinds["Label"] = 10; // need a better option for this.
            _kinds["Keyword"] = 13;
            _kinds["Namespace"] = 8;
        }

        internal static int Convert(string kind)
        {
            int value = 9;
            _kinds.TryGetValue(kind, out value);
            return value;
        }
    }

    internal struct AutoCompleteItem : IJsonSerializable
    {
        /// <summary>
        /// The text to be "completed", that is, the text that will be inserted in the editor.
        /// </summary>
        public string CompletionText { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// The text that should be displayed in the auto-complete UI.
        /// </summary>
        public string DisplayText { get; set; }
        public string RequiredNamespaceImport { get; set; }
        public string MethodHeader { get; set; }
        public string ReturnType { get; set; }
        public string Snippet { get; set; }
        public string Kind { get; set; }

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            //注意：直接转换为前端需要的格式
            writer.WriteString("detail", string.IsNullOrEmpty(ReturnType) ?
                DisplayText : $"{ReturnType} {DisplayText}");
            writer.WriteString("documentation", Description); //TODO: extractSummaryText(response.Description)
            writer.WriteNumber("kind", KindHelper.Convert(Kind));
            writer.WriteString("insertText", CompletionText.Replace("<", "").Replace(">", ""));
            writer.WriteString("label", DisplayText);
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

    }

    [Flags]
    internal enum WantsType
    {
        None = 0,
        /// <summary>
        ///   Specifies whether to return the code documentation for
        ///   each and every returned autocomplete result.
        /// </summary>
        WantDocumentationForEveryCompletionResult = 1,
        /// <summary>
        ///   Specifies whether to return importable types. Defaults to
        ///   false. Can be turned off to get a small speed boost.
        /// </summary>
        WantImportableTypes = 2,
        /// <summary>
        /// Returns a 'method header' for working with parameter templating.
        /// </summary>
        WantMethodHeader = 4,
        /// <summary>
        /// Returns a snippet that can be used by common snippet libraries
        /// to provide parameter and type parameter placeholders
        /// </summary>
        WantSnippet = 8,
        /// <summary>
        /// Returns the return type
        /// </summary>
        WantReturnType = 16,
        /// <summary>
        /// Returns the kind (i.e Method, Property, Field)
        /// </summary>
        WantKind = 32
    }

    sealed class GetCompletion : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            int type = args.GetInt32(); //TODO: remove it
            string fileName = args.GetString();
            int line = args.GetInt32() - 1; //注意：前端传过来的值需要-1
            int column = args.GetInt32() - 1;
            string wordToComplete = args.GetString();
            WantsType wants = WantsType.WantDocumentationForEveryCompletionResult | WantsType.WantKind | WantsType.WantReturnType; //暂默认

            var completions = new HashSet<AutoCompleteItem>();

            var document = hub.TypeSystem.Workspace.GetOpenedDocumentByName(fileName);
            if (document == null)
                throw new Exception($"Cannot find opened document: {fileName}");

            var sourceText = await document.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(line, column));
            var service = CompletionService.GetService(document);
            var completionList = await service.GetCompletionsAsync(document, position);
            if (completionList != null)
            {
                // Only trigger on space if Roslyn has object creation items
                //if (request.TriggerCharacter == " " && !completionList.Items.Any(i => i.IsObjectCreationCompletionItem()))
                //{
                //    return completions;
                //}

                // get recommened symbols to match them up later with SymbolCompletionProvider
                var semanticModel = await document.GetSemanticModelAsync();
                var recommendedSymbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(semanticModel, position, hub.TypeSystem.Workspace);

                foreach (var item in completionList.Items)
                {
                    var completionText = item.DisplayText;
                    if (completionText.IsValidCompletionFor(wordToComplete))
                    {
                        var symbols = await item.GetCompletionSymbolsAsync(recommendedSymbols, document);
                        if (symbols.Any())
                        {
                            foreach (var symbol in symbols)
                            {
                                if (item.UseDisplayTextAsCompletionText())
                                {
                                    completionText = item.DisplayText;
                                }
                                else if (item.TryGetInsertionText(out var insertionText))
                                {
                                    completionText = insertionText;
                                }
                                else
                                {
                                    completionText = symbol.Name;
                                }

                                if (symbol != null)
                                {
                                    if ((wants & WantsType.WantSnippet) == WantsType.WantSnippet)
                                    {
                                        // foreach (var completion in MakeSnippetedResponses(request, symbol, completionText))
                                        // {
                                        //     completions.Add(completion);
                                        // }
                                    }
                                    else
                                    {
                                        completions.Add(MakeAutoCompleteResponse(wants, symbol, completionText));
                                    }
                                }
                            }

                            // if we had any symbols from the completion, we can continue, otherwise it means
                            // the completion didn't have an associated symbol so we'll add it manually
                            continue;
                        }

                        // for other completions, i.e. keywords, create a simple AutoCompleteResponse
                        // we'll just assume that the completion text is the same
                        // as the display text.
                        var response = new AutoCompleteItem()
                        {
                            CompletionText = item.DisplayText,
                            DisplayText = item.DisplayText,
                            Snippet = item.DisplayText,
                            Kind = (wants & WantsType.WantKind) == WantsType.WantKind ? item.Tags.First() : null
                        };

                        completions.Add(response);
                    }
                }
            }

            //todo: 处理overloads
            return completions
                .OrderByDescending(c => c.CompletionText.IsValidCompletionStartsWithExactCase(wordToComplete))
                .ThenByDescending(c => c.CompletionText.IsValidCompletionStartsWithIgnoreCase(wordToComplete))
                .ThenByDescending(c => c.CompletionText.IsCamelCaseMatch(wordToComplete))
                .ThenByDescending(c => c.CompletionText.IsSubsequenceMatch(wordToComplete))
                .ThenBy(c => c.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.CompletionText, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private AutoCompleteItem MakeAutoCompleteResponse(WantsType wants, ISymbol symbol, string completionText, bool includeOptionalParams = true)
        {
            var displayNameGenerator = new SnippetGenerator();
            displayNameGenerator.IncludeMarkers = false;
            displayNameGenerator.IncludeOptionalParameters = includeOptionalParams;

            var response = new AutoCompleteItem();
            response.CompletionText = completionText;

            // TODO: Do something more intelligent here
            response.DisplayText = displayNameGenerator.Generate(symbol);

            if ((wants & WantsType.WantDocumentationForEveryCompletionResult) == WantsType.WantDocumentationForEveryCompletionResult)
            {
                response.Description = DocumentationConverter.ConvertDocumentation(symbol.GetDocumentationCommentXml(), "\n"/*_formattingOptions.NewLine*/);
            }

            if ((wants & WantsType.WantReturnType) == WantsType.WantReturnType)
            {
                response.ReturnType = ReturnTypeFormatter.GetReturnType(symbol);
            }

            if ((wants & WantsType.WantKind) == WantsType.WantKind)
            {
                response.Kind = symbol.GetKind();
            }

            // if (request.WantSnippet)
            // {
            //     var snippetGenerator = new SnippetGenerator();
            //     snippetGenerator.IncludeMarkers = true;
            //     snippetGenerator.IncludeOptionalParameters = includeOptionalParams;
            //     response.Snippet = snippetGenerator.Generate(symbol);
            // }

            // if (request.WantMethodHeader)
            // {
            //     response.MethodHeader = displayNameGenerator.Generate(symbol);
            // }

            return response;
        }
    }
}