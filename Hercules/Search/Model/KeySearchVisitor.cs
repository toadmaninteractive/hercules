using Hercules.Documents;
using Hercules.Forms.Schema;
using Json;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Search
{
    public sealed class KeySearchVisitor : ISearchVisitor
    {
        readonly SearchResults results;
        private readonly IReadOnlySet<string> keys;
        string? documentId;

        private KeySearchVisitor(IReadOnlySet<string> keys, SearchResults results)
        {
            this.keys = keys;
            this.results = results;
        }

        public static SearchResults Search(FormSchema schema, IEnumerable<IDocument> documents, string key)
        {
            return Search(schema, documents, key.Yield().ToHashSet());
        }

        public static SearchResults Search(FormSchema schema, IEnumerable<IDocument> documents, IReadOnlySet<string> keys)
        {
            var visitor = new KeySearchVisitor(keys, new SearchResults());
            foreach (var doc in documents)
            {
                visitor.documentId = doc.DocumentId;
                var search = new SchemaJsonSearch(visitor);
                search.Visit(JsonPath.Empty, doc.Json, schema.RootType);

                var baseJson = CouchUtils.GetBase(doc.Json);
                if (baseJson != null && baseJson.TryGetValue("_id", out var baseId) && baseId.IsString)
                    visitor.VisitPath(new JsonPath(CouchUtils.HerculesBase, "_id"), SearchDataType.Key, baseId.AsString);
            }
            return visitor.results;
        }

        public void VisitPath(JsonPath path, SearchDataType type, string data)
        {
            if (type == SearchDataType.Key && keys.Contains(data))
                results.Add(new Reference(documentId!, path, data));
        }
    }
}
