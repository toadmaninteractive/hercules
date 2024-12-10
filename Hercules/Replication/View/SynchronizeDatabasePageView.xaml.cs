using Hercules.DB;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Data;

namespace Hercules.Replication.View
{
    /// <summary>
    /// Interaction logic for CompareDatabasesPageView.xaml
    /// </summary>
    [ViewModelType(typeof(SynchronizeDatabasePage))]
    public partial class SynchronizeDatabasePageView : UserControl
    {
        public SynchronizeDatabasePageView()
        {
            InitializeComponent();
        }

        private void RadMaskedTextInput_ValueChanged(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            UpdateFilters();
        }

        private void UpdateFilters()
        {
            var dataContext = (SynchronizeDatabasePage)DataContext;
            GridView.FilterDescriptors.SuspendNotifications();
            GridView.FilterDescriptors.Clear();
            var filter = new FilterDescriptor
            {
                Member = "DocumentId",
                Operator = FilterOperator.Contains,
                Value = dataContext.Filter,
            };
            var filter1 = new CompositeFilterDescriptor
            {
                LogicalOperator = FilterCompositionLogicalOperator.Or,
            };
            if (dataContext.ShowAdded)
            {
                filter1.FilterDescriptors.Add(new FilterDescriptor
                {
                    Member = "ChangeType",
                    Operator = FilterOperator.IsEqualTo,
                    Value = DocumentCommitType.Added,
                });
            }
            if (dataContext.ShowDeleted)
            {
                filter1.FilterDescriptors.Add(new FilterDescriptor
                {
                    Member = "ChangeType",
                    Operator = FilterOperator.IsEqualTo,
                    Value = DocumentCommitType.Deleted,
                });
            }
            if (dataContext.ShowModified)
            {
                filter1.FilterDescriptors.Add(new FilterDescriptor
                {
                    Member = "ChangeType",
                    Operator = FilterOperator.IsEqualTo,
                    Value = DocumentCommitType.Modified,
                });
            }
            GridView.FilterDescriptors.Add(filter);
            GridView.FilterDescriptors.Add(filter1);
            GridView.FilterDescriptors.ResumeNotifications();
        }

        private void ToggleFilter_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateFilters();
        }
    }
}
