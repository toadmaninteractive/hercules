using Hercules.Documents;
using Hercules.Shortcuts;
using System.Collections.Generic;
using System.Windows;

namespace Hercules
{
    public static class HerculesDragData
    {
        public const string DragDataFormat = "Hercules";

        public static string? DragDocumentId(IDataObject dataObject)
        {
            if (dataObject.GetDataPresent(DragDataFormat))
            {
                var data = dataObject.GetData(DragDataFormat);
                if (data is IShortcutProvider shortcutProvider)
                {
                    if (shortcutProvider.Shortcut is DocumentShortcut docShortcut)
                        return docShortcut.DocumentId;
                }
            }
            return null;
        }

        public static IDocument? DragDocument(IReadOnlyDatabase database, IDataObject dataObject)
        {
            if (dataObject.GetDataPresent(DragDataFormat))
            {
                var docId = DragDocumentId(dataObject);
                if (docId != null)
                    return database.Documents.GetValueOrDefault(docId);
            }
            return null;
        }
    }
}
