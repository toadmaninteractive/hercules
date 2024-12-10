using Hercules.Documents.Editor;
using Hercules.Forms.Presentation;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for FormEditor.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentFormTab))]
    public partial class DocumentFormTabView : UserControl
    {
        public DocumentFormTab ViewModel => (DocumentFormTab)DataContext;

        public DocumentFormTabView()
        {
            InitializeComponent();
            this.IsVisibleChanged += UserControl_IsVisibleChanged;
        }

        private void ScrollViewer_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            /*
            var isMenu = VisualTreeHelperEx.GetParentTree(e.OldFocus as DependencyObject).OfType<MenuBase>().FirstOrDefault() != null;
            if (!isMenu)
            {
                var el = GetElementForControl(e.OriginalSource as FrameworkElement, sender);
                if (el != null)
                    ViewModel.Form.Select(el);
            }*/
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dobj && VirtualRowItem.GetItem(dobj) is { } item && item.IsSelected)
                return;

            if (e.OriginalSource is TextView avalonTextView)
            {
                if ((avalonTextView.ActualHeight + avalonTextView.VerticalOffset >= avalonTextView.DocumentHeight) || avalonTextView.VerticalOffset == 0)
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta / 5);
            }
            else if (e.OriginalSource is FrameworkElement originalElement)
            {
                if (!VisualTreeHelperEx.GetParentTree(originalElement).Contains(Scroller))
                    return;
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            /*
            var viewModel = ViewModel;
            if ((bool)e.NewValue && viewModel != null && viewModel.Editor.IsActive)
            {
                void BringSelectedElementIntoView()
                {
                    var selectedItem = ViewModel.Presentation.SelectedItem;
                    if (selectedItem != null)
                    {
                        Form_OnScrollIntoView(selectedItem.Row);
                        Form_OnFocusItem(selectedItem);
                    }
                }
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(BringSelectedElementIntoView));
            }*/
        }
    }
}