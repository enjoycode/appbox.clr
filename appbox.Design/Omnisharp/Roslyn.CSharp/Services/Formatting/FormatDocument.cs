using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Mef;
using OmniSharp.Roslyn.CSharp.Workers.Formatting;

namespace OmniSharp.Roslyn.CSharp.Services
{

    internal sealed class FormatDocument : IRequestHandler
    {

        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var fileName = (string)args.GetObject();
            var document = hub.TypeSystem.Workspace.GetOpenedDocumentByName(fileName);
            if (document == null)
                throw new Exception(string.Format("Cannot find opened document: {0}", fileName));

            var changes = FormattingWorker.GetFormattedTextChanges(document).Result;
            return Task.FromResult<object>(changes.ToArray());
        }
    }

}
