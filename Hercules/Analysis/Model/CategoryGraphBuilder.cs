using Hercules.Documents;
using Hercules.Search;
using Json;
using System.Collections.Generic;

namespace Hercules.Analysis
{
    public class CategoryGraphBuilder : ISearchVisitor
    {
        public GraphModel<Category> Graph { get; }
        private readonly GraphNode<Category>? currentNode;
        private readonly SchemafulDatabase database;

        public static GraphModel<Category> Build(SchemafulDatabase database, IReadOnlyList<Category> categories)
        {
            var builder = new CategoryGraphBuilder(database, categories);
            return builder.Graph;
        }

        public CategoryGraphBuilder(SchemafulDatabase database, IReadOnlyList<Category> categories)
        {
            this.database = database;
            Graph = new GraphModel<Category>(c => c.Name);
            Graph.AddNodes(categories);
            foreach (var cat in categories)
            {
                currentNode = Graph.Nodes[cat.Name];
                foreach (var doc in cat.Documents)
                {
                    var search = new SchemaJsonSearch(this);
                    search.Visit(JsonPath.Empty, doc.Json, database.Schema.RootType);

                    var baseJson = CouchUtils.GetBase(doc.Json);
                    if (baseJson != null && baseJson.TryGetValue("_id", out var baseId) && baseId.IsString)
                        VisitPath(new JsonPath(CouchUtils.HerculesBase, "_id"), SearchDataType.Key, baseId.AsString);
                }
            }
        }

        public void VisitPath(JsonPath path, SearchDataType type, string data)
        {
            if (type == SearchDataType.Key)
            {
                if (database.AllDocuments.TryGetValue(data, out var doc))
                {
                    var cat = database.GetDocumentCategory(doc);
                    if (cat.IsSchemaful && Graph.Nodes.TryGetValue(cat.Name, out var node))
                    {
                        Graph.AddReference(currentNode!, node);
                    }
                }
            }
        }
    }
}
