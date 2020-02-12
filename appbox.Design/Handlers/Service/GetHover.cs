using System;
using System.Threading;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class GetHover : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string fileName = args.GetString();
            int line = args.GetInt32();
            int column = args.GetInt32();

            var doc = hub.TypeSystem.Workspace.GetOpenedDocumentByName(fileName);
            if (doc == null)
                throw new Exception(string.Format("Cannot find opened document: {0}", fileName));

            //获取当前Symbol
            var semanticModel = await doc.GetSemanticModelAsync();
            var sourceText = await doc.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(line - 1, column - 1)); //注意：前端传过来的值需要-1
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, hub.TypeSystem.Workspace);
            if (symbol == null)
                return null;

            //判断当前是否在调试暂停中, TODO:判断是否调试目标调试服务
            if (hub.DebugService.IsPause)
            {
                var ct = new CancellationToken();
                var tipInfo = await DataTipInfoGetter.GetInfoAsync(doc, position, ct);
                var text = tipInfo.Text;
                if (text == null && !tipInfo.IsDefault)
                    text = sourceText.GetSubText(tipInfo.Span).ToString();
                var root = await semanticModel.SyntaxTree.GetRootAsync(ct).ConfigureAwait(false);
                var syntaxNode = root.FindNode(tipInfo.Span);
                if (syntaxNode == null)
                {
                    //Log.Warn($"SyntaxNode is null, 表达式:{tipInfo.Text}");
                    tipInfo = new DebugDataTipInfo(tipInfo.Span, text);
                }
                else
                {
                    tipInfo = DataTipInfoGetter.GetInfo(root, semanticModel, syntaxNode, text, ct);
                    //Log.Warn($"SyntaxNode: {syntaxNode} type:{syntaxNode.GetType().Name} 表达式:{tipInfo.Text}");
                }

                //TODO:暂简单实现，判断symbol是否变量
                if (symbol is ILocalSymbol || symbol is IParameterSymbol || symbol is IPropertySymbol || symbol is IFieldSymbol)
                {
                    var expression = tipInfo.Text;
                    //expression = "var emp = __context.emp;\nemp.ToString()";
                    //TODO: 根据虚拟类型转换请求，重新实现SymbolReader
                    //https://github.com/Samsung/netcoredbg/issues/28 netcoredbg暂无法计算emp.ToJson()，即无法计算方法
                    //var typeSymbol = TypeHelper.GetSymbolType(symbol) as INamedTypeSymbol;
                    //if (TypeHelper.IsEntityClass(typeSymbol))
                    //{
                    //}

                    var waithandler = new AutoResetEvent(false);
                    string symbolValue = "取值超时";
                    hub.DebugService.Evaluate(expression, res =>
                    {
                        symbolValue = res;
                        waithandler.Set();
                    });
                    waithandler.WaitOne(5000);

                    var type = TypeHelper.GetSymbolType(symbol);
                    return new Hover()
                    {
                        StartLine = line,
                        StartColumn = column,
                        EndLine = line,
                        EndColumn = column,
                        Contents = new object[] { $"**{type}**", symbolValue }
                    };
                }
                return null;
            }

            return new Hover()
            {
                StartLine = line,
                StartColumn = column,
                EndLine = line,
                EndColumn = column,
                Contents = new object[] { symbol.ToDisplayString() }
            };
        }

        internal struct Hover : IJsonSerializable
        {
            public int StartLine;
            public int StartColumn;
            public int EndLine;
            public int EndColumn;
            public object[] Contents;

            public PayloadType JsonPayloadType => PayloadType.UnknownType;

            public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

            public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
            {
                writer.WritePropertyName("contents");
                writer.WriteStartArray();
                for (int i = 0; i < Contents.Length; i++)
                {
                    writer.WriteStartObject();
                    writer.WriteString("value", (string)Contents[i]);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("range");
                writer.WriteStartObject();
                writer.WriteNumber("startLineNumber", StartLine);
                writer.WriteNumber("startColumn", StartColumn);
                writer.WriteNumber("endLineNumber", EndLine);
                writer.WriteNumber("endColumn", EndColumn);
                writer.WriteEndObject();
            }
        }
    }
}
