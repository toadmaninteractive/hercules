using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class LinkToDocumentNotification : Notification
    {
        public IReadOnlyList<IDocument> Documents { get; }

        IDocument? document;

        public IDocument? Document
        {
            get => document;
            set => SetField(ref document, value);
        }

        public ICommand LinkCommand { get; private set; }

        public LinkToDocumentNotification(IReadOnlyList<IDocument> documents, Action<IDocument> linkAction, object? source)
            : base(source)
        {
            Documents = documents;
            LinkCommand = Commands.Execute(() => { Close(); linkAction(Document!); }).If(() => Document != null);
        }
    }
}
