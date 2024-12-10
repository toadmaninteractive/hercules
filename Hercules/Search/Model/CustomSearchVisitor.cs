using Hercules.Documents;
using Hercules.Forms.Schema;
using Json;
using System.Collections.Generic;

namespace Hercules.Search
{
    public class CustomSearchVisitor : ISearchVisitor
    {
        public SearchResults Results { get; private set; }
        public string Text { get; set; }
        public bool SearchKeys { get; set; }
        public bool SearchText { get; set; }
        public bool SearchEnums { get; set; }
        public bool SearchNumbers { get; set; }
        public bool SearchFields { get; set; }
        public bool MatchCase { get; set; }
        public bool WholeWord { get; set; }

        string? documentId;
        double? number;

        public CustomSearchVisitor()
        {
            this.Results = new SearchResults();
            this.Text = string.Empty;
            this.SearchKeys = true;
            this.SearchText = true;
            this.SearchEnums = true;
            this.SearchNumbers = true;
            this.SearchFields = false;
        }

        public void ClearResults()
        {
            Results = new SearchResults();
        }

        public void Search(FormSchema schema, IEnumerable<IDocument> documents)
        {
            if (SearchNumbers)
                number = Numbers.ParseDouble(Text);

            foreach (var doc in documents)
            {
                documentId = doc.DocumentId;
                if (SearchKeys && MatchString(documentId))
                    Results.AddDocument(documentId);
                var search = new SchemaJsonSearch(this);
                search.Visit(JsonPath.Empty, doc.Json, schema.RootType); // TODO: FIXME
            }
        }

        public void VisitPath(JsonPath path, SearchDataType type, string data)
        {
            bool result = false;
            switch (type)
            {
                case SearchDataType.Text:
                    result = SearchText && MatchString(data);
                    break;

                case SearchDataType.Key:
                    result = SearchKeys && MatchString(data);
                    break;

                case SearchDataType.Enum:
                    result = SearchEnums && MatchString(data);
                    break;

                case SearchDataType.Number:
                    result = SearchNumbers && MatchNumber(data);
                    break;

                case SearchDataType.Field:
                    result = SearchFields && MatchString(data);
                    break;
            }
            if (result)
                Results.Add(new Reference(documentId!, path, data));
        }

        bool MatchString(string value)
        {
            return SearchHelper.MatchString(value, Text, MatchCase, WholeWord);
        }

        bool MatchNumber(string value)
        {
            return SearchHelper.MatchNumber(value, number);
        }
    }
}
