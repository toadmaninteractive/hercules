using Microsoft.Msagl.WpfGraphControl;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Analysis.View
{
    /// <summary>
    /// Interaction logic for CategoryGraphPageView.xaml
    /// </summary>
    [ViewModelType(typeof(CategoryGraphPage))]
    public partial class CategoryGraphPageView : UserControl
    {
        public CategoryGraphPageView()
        {
            InitializeComponent();
            Loaded += CategoryGraphPageView_Loaded;
        }

        private readonly GraphViewer graphViewer = new GraphViewer();

        private void CategoryGraphPageView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CategoryGraphPageView_Loaded;
            graphViewer.BindToPanel(GraphPanel);
            ((CategoryGraphPage)DataContext).PropertyChanged += ReferenceGraphPageView_PropertyChanged;
            Refresh();
        }

        private void ReferenceGraphPageView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryGraphPage.Graph))
                Refresh();
        }

        private void Refresh()
        {
            if (DataContext is CategoryGraphPage dataContext)
            {
                graphViewer.Graph = dataContext.Graph;
            }
        }
    }
}
