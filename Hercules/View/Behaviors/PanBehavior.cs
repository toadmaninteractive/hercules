using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class PanBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty MouseButtonProperty = DependencyProperty.Register("MouseButton", typeof(MouseButton), typeof(PanBehavior), new PropertyMetadata(MouseButton.Left));
        public static readonly DependencyProperty PanCommandProperty = DependencyProperty.Register("PanCommand", typeof(ICommand<Vector>), typeof(PanBehavior), new PropertyMetadata(null));

        public ICommand<Vector>? PanCommand
        {
            get => (ICommand<Vector>)GetValue(PanCommandProperty);
            set => SetValue(PanCommandProperty, value);
        }

        public MouseButton MouseButton
        {
            get => (MouseButton)GetValue(MouseButtonProperty);
            set => SetValue(MouseButtonProperty, value);
        }

        Point previousPosition;

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseDown += PreviewMouseDownHandler;
            AssociatedObject.PreviewMouseUp += PreviewMouseUpHandler;
            AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseDown -= PreviewMouseDownHandler;
            AssociatedObject.PreviewMouseUp -= PreviewMouseUpHandler;
            AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!AssociatedObject.IsMouseCaptured)
                return;
            var panCommand = PanCommand;
            if (panCommand != null && GetMouseButtonState(e, MouseButton) == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition((UIElement)AssociatedObject.Parent);
                var diff = currentPosition - previousPosition;
                if (diff.Length > 0)
                {
                    if (panCommand.CanExecute(diff))
                        panCommand.Execute(diff);
                    previousPosition = currentPosition;
                }
                e.Handled = true;
            }
        }

        static MouseButtonState GetMouseButtonState(MouseEventArgs e, MouseButton button)
        {
            return button switch
            {
                MouseButton.Right => e.RightButton,
                MouseButton.Middle => e.MiddleButton,
                MouseButton.XButton1 => e.XButton1,
                MouseButton.XButton2 => e.XButton2,
                _ => e.LeftButton,
            };
        }

        void PreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (!AssociatedObject.IsMouseCaptured)
                return;
            if (GetMouseButtonState(e, MouseButton) != MouseButtonState.Pressed)
            {
                Mouse.OverrideCursor = default(Cursor);
                AssociatedObject.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        void PreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (GetMouseButtonState(e, MouseButton) == MouseButtonState.Pressed)
            {
                previousPosition = e.GetPosition((UIElement)AssociatedObject.Parent);
                Mouse.OverrideCursor = Cursors.Hand;
                AssociatedObject.CaptureMouse();
                e.Handled = true;
            }
        }
    }
}
