using Hercules.Shell;
using Json;
using System;

namespace Hercules.Search
{
    public class SearchResultsTool : Tool
    {
        private readonly Action<string, JsonPath?> openDocumentAction;

        public SearchResultsTool(Action<string, JsonPath?> openDocumentAction)
        {
            this.openDocumentAction = openDocumentAction;
            this.Title = "Search Results";
            this.ContentId = "{SearchResults}";
            this.Pane = "BottomToolsPane";
            this.IsVisible = false;
        }

        SearchResults? results;

        public SearchResults? Results
        {
            get => results;
            set => SetField(ref results, value);
        }

        public void OpenDocument(string documentId, JsonPath? path) => openDocumentAction(documentId, path);
    }
}
