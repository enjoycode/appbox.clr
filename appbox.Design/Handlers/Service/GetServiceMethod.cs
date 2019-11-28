using System;
using System.Text;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 用于前端调试服务方法或测试调用服务方法时定位服务方法及获取参数
    /// </summary>
    sealed class GetServiceMethod : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var line = args.GetInt32() - 1;
            var column = args.GetInt32() - 1;

            var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception("Can't find service model node");

            //定位服务入口方法
            var doc = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(modelNode.RoslynDocumentId);
            var semanticModel = await doc.GetSemanticModelAsync();
            var sourceText = await doc.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(line, column));
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, hub.TypeSystem.Workspace);
            if (symbol == null)
                throw new Exception("Can't find service method");
            if (symbol.Kind != SymbolKind.Method)
                throw new Exception("Not a method");
            if (symbol.ContainingType.ToString() != string.Format("{0}.ServiceLogic.{1}", modelNode.AppNode.ID, modelNode.Model.Name))
                throw new Exception("Not a service method");
            if (symbol.DeclaredAccessibility.ToString() != "Public")
                throw new Exception("Not a service method");

            IMethodSymbol method = symbol as IMethodSymbol;
            var sb = new StringBuilder("{\"Name\":\"");
            sb.Append(method.Name);
            sb.Append("\", \"Args\":[");
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                sb.AppendFormat("{{\"Name\":\"{0}\",\"Type\":\"{1}\"}}", method.Parameters[i].Name, method.Parameters[i].Type);
                if (i != method.Parameters.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
