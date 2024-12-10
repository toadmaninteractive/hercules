using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class DragSourceBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(DragSourceBehavior));

        public static readonly DependencyProperty IsDraggedProperty = DependencyProperty.Register("IsDragged", typeof(bool), typeof(DragSourceBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        bool isDragged = false;

        public bool IsDragged
        {
            get => isDragged;
            set { }
        }

        void SetIsDragged(bool value)
        {
            isDragged = value;
            SetValue(IsDraggedProperty, value);
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
                    DragStarted();
                }
            }
        }

        void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AssociatedObject.CaptureMouse();
            dragInit = true;
            startPoint = e.GetPosition(AssociatedObject);
            e.Handled = true;
        }

        void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragInit)
            {
                dragInit = false;
                Mouse.Capture(null);
                e.Handled = true;
            }
        }

        void DragStarted()
        {
            dragInit = false;
            SetIsDragged(true);
            DataObject data = new DataObject(HerculesDragData.DragDataFormat, Data);
            DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
            SetIsDragged(false);
        }
    }
}
