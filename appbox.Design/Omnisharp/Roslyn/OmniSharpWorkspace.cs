using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Utilities;

namespace OmniSharp
{
    internal sealed class OmniSharpWorkspace : Workspace
    {

        public bool Initialized { get; set; }
        // public BufferManager BufferManager { get; private set; }

        public OmniSharpWorkspace(HostServicesAggregator aggregator) : base(aggregator.CreateHostServices(), "Custom")
        {
            // BufferManager = new BufferManager(this);
        }

        public override bool CanOpenDocuments => true;

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            return true;
        }

        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            var doc = CurrentSolution.GetDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                OnDocumentOpened(documentId, text.Container, activate);
            }
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var doc = CurrentSolution.GetDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                var version = doc.GetTextVersionAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                OnDocumentClosed(documentId, loader);
            }
        }

        public void OnDocumentChanged(DocumentId documentId, SourceText text)
        {
            OnDocumentTextChanged(documentId, text, PreservationMode.PreserveIdentity);
        }

        public Document GetOpenedDocumentByName(string fileName)
        {
            foreach (var id in GetOpenDocumentIds())
            {
                var doc = CurrentSolution.GetDocument(id);
                if (doc.Name == fileName)
                    return doc;
            }
            return null;
        }

    }

}