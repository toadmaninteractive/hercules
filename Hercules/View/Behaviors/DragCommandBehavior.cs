using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class DragCommandBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty DragCommandProperty = DependencyProperty.Register("DragCommand", typeof(ICommand), typeof(DragCommandBehavior), new PropertyMetadata(null));

        public ICommand DragCommand
        {
            get => (ICommand)GetValue(DragCommandProperty);
            set => SetValue(DragCommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
            AssociatedObject.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += PreviewMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonUp -= PreviewMouseLeftButtonUp;
            AssociatedObject.PreviewMouseLeftButtonDown -= PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= PreviewMouseMove;
        }

        bool dragInit;
        bool isDragging;
        Point startPoint;

        void PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (dragInit)
            {
                var position = e.GetPosition(AssociatedObject);
                if ((Math.Abs(position.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(position.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    e.Handled = true;
                    AssociatedObject.CaptureMouse();
                    DragStarted();
                }
            }
        }

        void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DragCommand == null || !DragCommand.CanExecute(AssociatedObject))
                return;
            isDragging = false;
            dragInit = true;
            startPoint = e.GetPosition(AssociatedObject);
        }

        void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragInit)
            {
                dragInit = false;
                if (isDragging)
                {
                    Mouse.Capture(null);
                    e.Handled = true;
                }
            }
            isDragging = false;
        }

        void DragStarted()
        {
            dragInit = false;
            isDragging = true;
            DragCommand.Execute(AssociatedObject);
        }
    }
}
