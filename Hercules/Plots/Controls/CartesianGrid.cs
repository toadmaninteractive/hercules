using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class CartesianGrid : CartesianRenderer
    {
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(CartesianGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(CartesianGrid), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.Register("ClickCommand", typeof(ICommand), typeof(CartesianGrid), new FrameworkPropertyMetadata(null));

        public ICommand? DoubleClickCommand
        {
            get => (ICommand)GetValue(DoubleClickCommandProperty);
            set => SetValue(DoubleClickCommandProperty, value);
        }
        public ICommand? ClickCommand
        {
            get => (ICommand)GetValue(ClickCommandProperty);
            set => SetValue(ClickCommandProperty, value);
        }

        private Geometry? horzLine;
        private Geometry? vertLine;

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var panel = (CartesianPanel)VisualParent;
            var renderRect = new Rect(RenderSize);
            var xBestFit = Math.Pow(10, Math.Round(Math.Log10(panel.Scale.X)));
            var yBestFit = Math.Pow(10, Math.Round(Math.Log10(panel.Scale.Y)));
            drawingContext.DrawRectangle(GetHorzBrush(panel, 10 / xBestFit), null, renderRect);
            drawingContext.DrawRectangle(GetHorzBrush(panel, 100 / xBestFit), null, renderRect);
            drawingContext.DrawRectangle(GetHorzBrush(panel, 1000 / xBestFit), null, renderRect);
            drawingContext.DrawRectangle(GetVertBrush(panel, 10 / yBestFit), null, renderRect);
            drawingContext.DrawRectangle(GetVertBrush(panel, 100 / yBestFit), null, renderRect);
            drawingContext.DrawRectangle(GetVertBrush(panel, 1000 / yBestFit), null, renderRect);
        }

        private TileBrush GetHorzBrush(CartesianPanel panel, double interval)
        {
            return GetBrush(panel.ZeroPosition.X, panel.Scale.X * interval, GetHorzLine());
        }

        private TileBrush GetVertBrush(CartesianPanel panel, double interval)
        {
            return GetBrush(panel.ZeroPosition.Y, panel.Scale.Y * interval, GetVertLine());
        }

        private TileBrush GetBrush(double zeroPosition, double viewportSize, Geometry geometry)
        {
            var offset = (zeroPosition + viewportSize * 0.5) % viewportSize;
            var realThickness = Math.Max(Math.Min(viewportSize * 0.01, 2.0), 0.1);
            var thickness = realThickness / viewportSize;
            var drawing = new GeometryDrawing(null, GetPen(thickness), geometry);
            drawing.Freeze();
            return new DrawingBrush(drawing)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(offset, offset, viewportSize, viewportSize),
                Stretch = Stretch.Uniform,
                ViewboxUnits = BrushMappingMode.Absolute
            };
        }

        private Geometry GetHorzLine()
        {
            if (horzLine == null)
            {
                horzLine = new LineGeometry(new Point(0.5, 0), new Point(0.5, 1));
                horzLine.Freeze();
            }
            return horzLine;
        }

        private Geometry GetVertLine()
        {
            if (vertLine == null)
            {
                vertLine = new LineGeometry(new Point(0, 0.5), new Point(1, 0.5));
                vertLine.Freeze();
            }
            return vertLine;
        }

        private Pen GetPen(double thickness)
        {
            var pen = new Pen
            {
                Thickness = thickness,
                Brush = Stroke
            };
            pen.Freeze();
            return pen;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var command = e.ClickCount == 2 ? DoubleClickCommand : ClickCommand;

            if (command != null)
            {
                var pos = Panel.RenderToModel(e.GetPosition(this));
                if (command.CanExecute(pos))
                    command.Execute(pos);
                e.Handled = true;
            }
            base.OnPreviewMouseLeftButtonDown(e);
        }
    }
}
