using Hercules.Documents;
using Hercules.Shell;
using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Analysis
{
    public class ReferenceGraphCategory : NotifyPropertyChanged
    {
        private readonly Action refresh;
        public Category Category { get; }

        private bool enabled = true;

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (SetField(ref enabled, value))
                    refresh();
            }
        }

        public System.Windows.Media.SolidColorBrush Color { get; }

        public ReferenceGraphCategory(Category category, System.Windows.Media.SolidColorBrush color, Action refresh)
        {
            this.refresh = refresh;
            Category = category;
            Color = color;
        }
    }

    public class ReferenceGraphPage : Page
    {
        private readonly Project project;
        private IReadOnlyCollection<IDocument> rootDocuments;
        public IBulkCommand<IDocument> EditDocumentCommand { get; }

        public ObservableCollection<ReferenceGraphCategory> Categories { get; }
        public Project Project => project;

        private Graph? graph;
        public Graph? Graph
        {
            get => graph;
            set => SetField(ref graph, value);
        }
        public ICommand RefreshCommand { get; }
        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }
        private bool isBulkUpdate;

        private int referencesDepth = 1;
        public int ReferencesDepth
        {
            get => referencesDepth;
            set
            {
                if (SetField(ref referencesDepth, value))
                {
                    Refresh();
                }
            }
        }

        private int referencedByDepth = 1;
        public int ReferencedByDepth
        {
            get => referencedByDepth;
            set
            {
                if (SetField(ref referencedByDepth, value))
                {
                    Refresh();
                }
            }
        }

        public ReferenceGraphPage(Project project, IReadOnlyCollection<IDocument> rootDocuments, IBulkCommand<IDocument> editDocumentCommand)
        {
            Title = "Reference Graph";
            RefreshCommand = Commands.Execute(Refresh);
            CheckAllCommand = Commands.Execute(() => CheckAll(true));
            UncheckAllCommand = Commands.Execute(() => CheckAll(false));
            this.project = project;
            this.rootDocuments = rootDocuments;
            this.EditDocumentCommand = editDocumentCommand;
            this.Categories = new ObservableCollection<ReferenceGraphCategory>(project.SchemafulDatabase.Categories.Select((c, i) => new ReferenceGraphCategory(c, ColorPalette.GetBrush(i), Refresh)));
            Refresh();
        }

        public void Refresh()
        {
            if (isBulkUpdate)
                return;

            if (project.SchemafulDatabase.Schema == null)
            {
                Graph = null;
                return;
            }

            var buildGraph = new Graph("Reference Graph");
            buildGraph.Attr.BackgroundColor = Color.AliceBlue;

            IReadOnlyList<IDocument> filterDocuments = project.SchemafulDatabase.SchemafulDocuments;
            if (Categories.Count > 0)
            {
                var docs = rootDocuments.ToList();
                foreach (var cat in Categories)
                {
                    if (!cat.Enabled)
                        continue;

                    foreach (var doc in cat.Category.Documents)
                    {
                        if (!rootDocuments.Contains(doc))
                            docs.Add(doc);
                    }
                }
                filterDocuments = docs;
            }

            var graphModel = ReferenceGraphBuilder.Build(project.SchemafulDatabase.Schema, filterDocuments);

            var rootNodes = new HashSet<GraphNode<IDocument>>();

            foreach (var document in rootDocuments)
            {
                if (graphModel.Nodes.TryGetValue(document.DocumentId, out var rootNode))
                {
                    var node = buildGraph.AddNode(document.DocumentId);
                    node.Attr.LabelMargin = 10;
                    node.Attr.Color = Color.DarkGreen;
                    node.Attr.LineWidth = 2;
                    node.UserData = document;
                    rootNodes.Add(rootNode);
                }
            }

            foreach (var edgeModel in graphModel.TraverseReferences(rootNodes, referencesDepth))
            {
                var edge = buildGraph.AddEdge(edgeModel.from.Id, edgeModel.to.Id);
                edge.Attr.Color = Color.Gray;
                edge.Attr.LineWidth = 0.5;
            }

            foreach (var edgeModel in graphModel.TraverseReferencedBy(rootNodes, referencedByDepth))
            {
                var edge = buildGraph.AddEdge(edgeModel.from.Id, edgeModel.to.Id);
                edge.Attr.Color = Color.Gray;
                edge.Attr.LineWidth = 0.5;
            }

            foreach (var cat in Categories)
            {
                if (!cat.Enabled)
                    continue;
                foreach (var doc in cat.Category.Documents)
                {
                    var node = buildGraph.FindNode(doc.DocumentId);
                    if (node != null)
                    {
                        node.Attr.FillColor = new Color(cat.Color.Color.R, cat.Color.Color.G, cat.Color.Color.B);
                        node.Attr.LabelMargin = rootDocuments.Contains(doc) ? 10 : 2;
                        node.UserData = doc;
                    }
                }
            }

            Graph = buildGraph;
        }

        private void CheckAll(bool value)
        {
            isBulkUpdate = true;
            bool needRefresh = false;
            try
            {
                foreach (var cat in Categories)
                {
                    needRefresh |= cat.Enabled != value;
                    cat.Enabled = value;
                }
            }
            finally
            {
                isBulkUpdate = false;
            }
            if (needRefresh)
                Refresh();
        }

        public void SetRootDocument(IDocument document)
        {
            rootDocuments = new[] { document };
            Refresh();
        }
    }
}
