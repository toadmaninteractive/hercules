using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class CartesianThumb : Control
    {
        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(CartesianThumb), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(CartesianThumb), new FrameworkPropertyMetadata(false));

        public ICommand? DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        Point previousDragPosition;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete && !IsReadOnly)
            {
                if (DeleteCommand != null && DeleteCommand.CanExecute(null))
                {
                    DeleteCommand.Execute(null);
                    e.Handled = true;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsReadOnly)
                return;
            var control = VisualTreeHelperEx.GetParent<CartesianControl>(this);
            if (control == null)
                return;
            previousDragPosition = e.GetPosition(null);
            Focus();
            CaptureMouse();
            e.Handled = true;
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
                e.Handled = true;
            }
            base.OnPreviewMouseLeftButtonUp(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                var control = VisualTreeHelperEx.GetParent<CartesianControl>(this);
                if (control != null)
                {
                    Point currentPosition = e.GetPosition(null);
                    var diff = currentPosition - previousDragPosition;
                    if (diff.Length > 0)
                    {
                        previousDragPosition = currentPosition;
                        control.MoveInViewport(diff);
                    }
                }
                e.Handled = true;
            }
            base.OnPreviewMouseMove(e);
        }
    }
}
