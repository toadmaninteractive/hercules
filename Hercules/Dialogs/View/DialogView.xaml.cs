using Hercules.Documents.Editor;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Dialogs.View
{
    [ViewModelType(typeof(DialogTab))]
    public partial class DialogView : UserControl
    {
        private DialogTab Context => (DialogTab)DataContext;

        public DialogView()
        {
            InitializeComponent();
        }

        private void SelectionChangedHandler(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems.OfType<BaseReplicaViewModel>().FirstOrDefault();
            var unselectedItem = e.RemovedItems.OfType<BaseReplicaViewModel>().FirstOrDefault();

            unselectedItem?.SetSelectedState(false);

            if (selectedItem != null)
            {
                selectedItem.SetSelectedState(true);
                Context.Editor.Properties.Value = new ElementProperties(Context.Editor, selectedItem.Element.RecordElement.Children);
            }
        }

        private void ChangeViewMode(object sender, MouseButtonEventArgs e)
        {
            var replica = ((Grid)sender).DataContext as ReplicaViewModel;
            if (replica == null)
                throw new InvalidOperationException("DataContext of sender must be ReplicaViewModel");

            if (Context.IsAnchorSelectState)
            {
                Context.IsAnchorSelectState = false;
                Context.AddVirtualRepliva(replica);
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
                replica.IsEditMode = !replica.IsEditMode;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
                Context.IsBlur = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
                Context.IsBlur = false;
            base.OnKeyUp(e);
        }
    }
}