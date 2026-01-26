using Hercules.Shell;
using Microsoft.Msagl.Drawing;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Analysis
{
    public class CategoryGraphPage : Page
    {
        private readonly Project project;

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

        public CategoryGraphPage(Project project)
        {
            Title = "Category Graph";
            RefreshCommand = Commands.Execute(Refresh);
            CheckAllCommand = Commands.Execute(() => CheckAll(true));
            UncheckAllCommand = Commands.Execute(() => CheckAll(false));
            this.project = project;
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

            var buildGraph = new Graph("Category Graph");
            buildGraph.Attr.BackgroundColor = Color.AliceBlue;

            var graphModel = CategoryGraphBuilder.Build(project.SchemafulDatabase, Categories.Where(c => c.Enabled).Select(c => c.Category).ToList());

            foreach (var cat in Categories)
            {
                if (cat.Enabled && graphModel.Nodes.TryGetValue(cat.Category.Name, out var nodeModel))
                {
                    var node = buildGraph.AddNode(nodeModel.Id);
                    node.Attr.LabelMargin = 10;
                    node.Attr.Color = Color.DarkGreen;
                    node.Attr.LineWidth = 2;
                    node.Attr.FillColor = new Color(cat.Color.Color.R, cat.Color.Color.G, cat.Color.Color.B); ;
                    node.UserData = nodeModel.Value;
                }
            }

            foreach (var nodeModel in graphModel.Nodes.Values)
            {
                foreach (var nodeModel2 in nodeModel.References)
                {
                    var edge = buildGraph.AddEdge(nodeModel.Id, nodeModel2.Id);
                    edge.Attr.Color = Color.Gray;
                    edge.Attr.LineWidth = 0.5;
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
    }
}
