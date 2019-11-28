using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Design;
using appbox.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Mef;
using appbox;

namespace OmniSharp.Roslyn.CSharp.Services
{
    sealed class ChangeBuffer : IRequestHandler
    {

        //todo:修改实现，获取Changes集合
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            int type = args.GetInt32();
            string targetID = args.GetString();
            int startLine = args.GetInt32() - 1; //注意：前端传过来的值需要-1
            int startColumn = args.GetInt32() - 1;
            int endLine = args.GetInt32() - 1;
            int endColumn = args.GetInt32() - 1;
            string newText = args.GetString();

            Document document = null;
            if (type == 1) //服务代码变更
            {
                var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(targetID));
                if (modelNode == null)
                    throw new Exception($"Cannot find ServiceModel: {targetID}");

                document = hub.TypeSystem.Workspace.CurrentSolution.GetDocument(modelNode.RoslynDocumentId);
            }
            else if (type == 2) //表达式代码变更
            {
                throw ExceptionHelper.NotImplemented();
                //document = hub.ExpressionDesignService.GetExpressionDocument(targetID);
            }
            else
            {
                throw ExceptionHelper.NotImplemented();
            }

            if (document == null)
                throw new Exception("Can not find opened document: " + targetID);

            var sourceText = await document.GetTextAsync();
            var startOffset = sourceText.Lines.GetPosition(new LinePosition(startLine, startColumn));
            var endOffset = sourceText.Lines.GetPosition(new LinePosition(endLine, endColumn));

            sourceText = sourceText.WithChanges(new[] {
                        new TextChange(new TextSpan(startOffset, endOffset - startOffset), newText)
                    });

            hub.TypeSystem.Workspace.OnDocumentChanged(document.Id, sourceText);
            return null;
        }
    }
}