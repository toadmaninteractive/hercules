using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    public class DropTargetBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty DropCommandProperty = DependencyProperty.Register("DropCommand", typeof(ICommand<IDataObject>), typeof(DropTargetBehavior), new PropertyMetadata(null));

        public ICommand<IDataObject> DropCommand
        {
            get => (ICommand<IDataObject>)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        private ScrollViewer? comboBoxEditableScrollViewer;

        protected override void OnAttached()
        {
            AssociatedObject.Drop += Drop;
            AssociatedObject.DragOver += DragOver;
            // Wild hack to allow dropping to editable combo box
            if (AssociatedObject is ComboBox comboBox)
            {
                comboBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                          (DispatcherOperationCallback)delegate
                          {
                              var childrenCount = VisualTreeHelper.GetChildrenCount(comboBox);
                              if (childrenCount > 0)
                              {
                                  var rootElement = VisualTreeHelper.GetChild(comboBox, 0) as FrameworkElement;
                                  TextBox textBox = (TextBox)rootElement.FindName("PART_EditableTextBox");
                                  if (textBox != null)
                                  {
                                      comboBoxEditableScrollViewer = VisualTreeHelperEx.GetDescendant<ScrollViewer>(textBox);
                                      if (comboBoxEditableScrollViewer != null)
                                      {
                                          comboBoxEditableScrollViewer.Drop += Drop;
                                          comboBoxEditableScrollViewer.DragOver += DragOver;
                                      }
                                  }
                              }
                              return null;
                          }
                          , null);
            }

            if (AssociatedObject is RadAutoSuggestBox autoSuggestBox)
            {
                autoSuggestBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    (DispatcherOperationCallback)delegate
                    {
                        var childrenCount = VisualTreeHelper.GetChildrenCount(autoSuggestBox);
                        if (childrenCount > 0)
                        {
                            var rootElement = VisualTreeHelper.GetChild(autoSuggestBox, 0) as FrameworkElement;
                            TextBox textBox = (TextBox)rootElement.FindName("PART_TextBox");
                            if (textBox != null)
                            {
                                comboBoxEditableScrollViewer = VisualTreeHelperEx.GetDescendant<ScrollViewer>(textBox);
                                if (comboBoxEditableScrollViewer != null)
                                {
                                    comboBoxEditableScrollViewer.Drop += Drop;
                                    comboBoxEditableScrollViewer.DragOver += DragOver;
                                }
                            }
                        }
                        return null;
                    }
                    , null);
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Drop -= Drop;
            AssociatedObject.DragOver -= DragOver;
            if (comboBoxEditableScrollViewer != null)
            {
                comboBoxEditableScrollViewer.Drop -= Drop;
                comboBoxEditableScrollViewer.DragOver -= DragOver;
                comboBoxEditableScrollViewer = null;
            }
        }

        void DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            var command = DropCommand;
            if (command != null)
            {
                if (command.CanExecute(e.Data))
                    e.Effects = DragDropEffects.All;
            }
            e.Handled = true;
        }

        void Drop(object sender, DragEventArgs e)
        {
            var command = DropCommand;
            if (command.CanExecute(e.Data))
                command.Execute(e.Data);
            e.Handled = true;
        }
    }
}
