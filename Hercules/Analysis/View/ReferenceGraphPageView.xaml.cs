using Hercules.Documents;
using Microsoft.Msagl.WpfGraphControl;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Analysis.View
{
    /// <summary>
    /// Interaction logic for ReferenceGraphPageView.xaml
    /// </summary>
    [ViewModelType(typeof(ReferenceGraphPage))]
    public partial class ReferenceGraphPageView : UserControl
    {
        public ReferenceGraphPageView()
        {
            InitializeComponent();
            Loaded += ReferenceGraphPageView_Loaded;
        }

        private readonly GraphViewer graphViewer = new GraphViewer();

        private void ReferenceGraphPageView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ReferenceGraphPageView_Loaded;
            graphViewer.BindToPanel(GraphPanel);
            graphViewer.MouseDown += GraphViewer_MouseDown;
            ((ReferenceGraphPage)DataContext).PropertyChanged += ReferenceGraphPageView_PropertyChanged;
            Refresh();
        }

        private void GraphViewer_MouseDown(object? sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
            e.Handled = false;
            if (e.LeftButtonIsPressed && e.Clicks > 1 && graphViewer.ObjectUnderMouseCursor?.DrawingObject?.UserData is IDocument document)
            {
                ((ReferenceGraphPage)DataContext).EditDocumentCommand.Single.Execute(document);
                e.Handled = true;
            }
            else if (e.RightButtonIsPressed && e.Clicks > 1 && graphViewer.ObjectUnderMouseCursor?.DrawingObject?.UserData is IDocument document1)
            {
                ((ReferenceGraphPage)DataContext).SetRootDocument(document1);
            }
        }

        private void ReferenceGraphPageView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReferenceGraphPage.Graph))
                Refresh();
        }

        private void Refresh()
        {
            if (DataContext is ReferenceGraphPage dataContext)
            {
                graphViewer.Graph = dataContext.Graph;
            }
        }
    }
}
