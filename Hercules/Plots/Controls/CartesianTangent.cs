using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Controls
{
    public enum CartesianTangentMode
    {
        LeftSlope,
        RightSlope,
    }

    public class CartesianTangent : Control, ICartesianVisual
    {
        public static readonly DependencyProperty TangentProperty = DependencyProperty.Register("Tangent", typeof(double), typeof(CartesianTangent), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender, OnTangentChanged));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(CartesianTangent), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, OnPenChanged));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CartesianTangent), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnPenChanged));

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register("Mode", typeof(CartesianTangentMode), typeof(CartesianTangent), new FrameworkPropertyMetadata(CartesianTangentMode.LeftSlope));

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(double), typeof(CartesianTangent), new FrameworkPropertyMetadata(40.0));

        public double Tangent
        {
            get => (double)GetValue(TangentProperty);
            set => SetValue(TangentProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public CartesianTangentMode Mode
        {
            get => (CartesianTangentMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private Pen? pen;

        private static void OnPenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CartesianTangent)d).pen = null;
        }

        private Pen GetPen()
        {
            if (pen == null)
                pen = new Pen(Stroke, StrokeThickness);
            return pen;
        }

        private static void OnTangentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CartesianTangent control)
            {
                control.SetPosition(control.Tangent);
                control.InvalidateArrange();
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            SetPosition(Tangent);
            InvalidateArrange();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
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
            if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                var parent = VisualTreeHelperEx.GetParent<IInputElement>(VisualTreeHelper.GetParent(this));
                Point pos = e.GetPosition(parent);
                Tangent = PointToTangent(pos);
                SetPosition(Tangent);
                e.Handled = true;
            }
            base.OnPreviewMouseMove(e);
        }

        public void InvalidateTransform()
        {
            SetPosition(Tangent);
            InvalidateVisual();
        }

        private double PointToTangent(Point pos)
        {
            var panel = VisualTreeHelperEx.GetParent<CartesianPanel>(this)!;
            if (Mode == CartesianTangentMode.LeftSlope)
            {
                if (pos.X <= 0)
                    return pos.Y >= 0 ? double.NegativeInfinity : double.PositiveInfinity;
                else
                    return -(pos.Y * panel.Scale.X) / (pos.X * panel.Scale.Y);
            }
            else
            {
                if (pos.X >= 0)
                    return pos.Y >= 0 ? double.NegativeInfinity : double.PositiveInfinity;
                else
                    return (pos.Y * panel.Scale.X) / (pos.X * panel.Scale.Y);
            }
        }

        private void SetPosition(double tangent)
        {
            var panel = VisualTreeHelperEx.GetParent<CartesianPanel>(this)!;
            Vector v;
            if (double.IsNaN(tangent))
            {
                v = new Vector(1, 0);
            }
            else if (double.IsPositiveInfinity(tangent))
            {
                v = new Vector(0, 1);
            }
            else if (double.IsNegativeInfinity(tangent))
            {
                v = new Vector(0, -1);
            }
            else
            {
                var stangent = tangent * panel.Scale.Y / panel.Scale.X;
                var len = Math.Sqrt(1 + stangent * stangent);
                v = new Vector(1 / len, stangent / len);
                v.Normalize();
            }
            v *= Size;
            if (Mode == CartesianTangentMode.LeftSlope)
            {
                Canvas.SetLeft(this, v.X);
                Canvas.SetTop(this, -v.Y);
            }
            else
            {
                Canvas.SetLeft(this, -v.X);
                Canvas.SetTop(this, -v.Y);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawLine(GetPen(), new Point(0, 0), new Point(-Canvas.GetLeft(this), -Canvas.GetTop(this)));
        }
    }
}
