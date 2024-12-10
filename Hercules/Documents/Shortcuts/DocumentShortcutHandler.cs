using Hercules.Shortcuts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Documents
{
    public class DocumentShortcutHandler : ShortcutHandler<DocumentShortcut>
    {
        private readonly ICommand<IDocument> editDocumentCommand;
        private readonly IReadOnlyObservableValue<Project?> observableProject;

        public DocumentShortcutHandler(IReadOnlyObservableValue<Project?> observableProject, ICommand<IDocument> editDocumentCommand)
        {
            this.observableProject = observableProject;
            this.editDocumentCommand = editDocumentCommand;
        }

        protected override Uri DoGetUri(DocumentShortcut shortcut)
        {
            return new Uri("hercules:////" + shortcut.DocumentId, UriKind.RelativeOrAbsolute);
        }

        protected override bool DoOpen(DocumentShortcut shortcut)
        {
            if (observableProject.Value != null)
            {
                if (observableProject.Value.Database.Documents.TryGetValue(shortcut.DocumentId, out var doc) && !doc.IsDesign)
                {
                    editDocumentCommand.Execute(doc);
                    return true;
                }
            }
            return false;
        }

        protected override string DoGetTitle(DocumentShortcut shortcut)
        {
            return shortcut.DocumentId;
        }

        protected override string DoGetIcon(DocumentShortcut shortcut)
        {
            return Fugue.Icons.BlueDocument;
        }

        protected override bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out DocumentShortcut shortcut)
        {
            if (uri.Segments.Length > 2)
            {
                shortcut = new DocumentShortcut(uri.Segments[2].TrimEnd('/'));
                return true;
            }

            shortcut = default!;
            return false;
        }
    }
}
