using Hercules.Controls.Tree;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Search.View
{
    [ViewModelType(typeof(SearchResultsTool))]
    public partial class SearchResultsToolView : UserControl
    {
        public SearchResultsToolView()
        {
            InitializeComponent();
        }

        private void TreeList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TreeList.SelectedItem is TreeNode item)
            {
                var model = (SearchResultsTool)DataContext;
                switch (item.Tag)
                {
                    case DocumentReferences refs:
                        model.OpenDocument(refs.DocumentId, refs.References.Count > 0 ? refs.References[0].Path : null);
                        break;
                    case Reference reference:
                        model.OpenDocument(reference.DocumentId, reference.Path);
                        break;
                }
            }
        }
    }
}
