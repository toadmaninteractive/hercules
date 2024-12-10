using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class CartesianControl : Control
    {
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(Point), typeof(CartesianControl), new FrameworkPropertyMetadata(new Point(0, 0), OnPositionChanged), Numbers.IsPoint);

        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CartesianControl control)
            {
                if (VisualTreeHelper.GetParent(control) is CartesianPanel panel)
                    panel.InvalidateArrange();
                control.OnPositionChanged();
            }
        }

        protected virtual void OnPositionChanged()
        {
        }

        public void MoveInViewport(Vector diff)
        {
            if (VisualParent is CartesianPanel panel)
            {
                var position = panel.ModelToRender(Position);
                position += diff;
                Position = panel.RenderToModel(position);
            }
        }
    }
}
