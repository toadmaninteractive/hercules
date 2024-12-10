using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Search;
using Json;
using System.Collections.Generic;

namespace Hercules.Analysis
{
    public class ReferenceGraphBuilder : ISearchVisitor
    {
        public GraphModel<IDocument> Graph { get; }
        private readonly GraphNode<IDocument>? currentNode;

        public static GraphModel<IDocument> Build(FormSchema schema, IReadOnlyList<IDocument> documents)
        {
            var builder = new ReferenceGraphBuilder(schema, documents);
            return builder.Graph;
        }

        public ReferenceGraphBuilder(FormSchema schema, IReadOnlyList<IDocument> documents)
        {
            Graph = new GraphModel<IDocument>(doc => doc.DocumentId);
            Graph.AddNodes(documents);
            foreach (var doc in documents)
            {
                currentNode = Graph.Nodes[doc.DocumentId];
                var search = new SchemaJsonSearch(this);
                search.Visit(JsonPath.Empty, doc.Json, schema.RootType);

                var baseJson = CouchUtils.GetBase(doc.Json);
                if (baseJson != null && baseJson.TryGetValue("_id", out var baseId) && baseId.IsString)
                    VisitPath(new JsonPath(CouchUtils.HerculesBase, "_id"), SearchDataType.Key, baseId.AsString);
            }
        }

        public void VisitPath(JsonPath path, SearchDataType type, string data)
        {
            if (type == SearchDataType.Key)
            {
                if (Graph.Nodes.TryGetValue(data, out var node))
                {
                    Graph.AddReference(currentNode!, node);
                }
            }
        }
    }
}
