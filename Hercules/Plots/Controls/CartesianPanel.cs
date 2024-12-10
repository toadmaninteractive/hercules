using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Controls
{
    public interface ICartesianVisual
    {
        void InvalidateTransform();
    }

    public class CartesianPanel : Panel
    {
        public static readonly DependencyProperty PanMouseButtonProperty = DependencyProperty.Register("PanMouseButton", typeof(MouseButton), typeof(CartesianPanel), new PropertyMetadata(MouseButton.Left));

        public static readonly DependencyProperty ViewportProperty = DependencyProperty.Register("Viewport", typeof(Rect), typeof(CartesianPanel), new FrameworkPropertyMetadata(OnViewportChanged));

        public static readonly DependencyProperty CanPanProperty = DependencyProperty.Register("CanPan", typeof(bool), typeof(CartesianPanel));

        public static readonly DependencyProperty CanZoomProperty = DependencyProperty.Register("CanZoom", typeof(bool), typeof(CartesianPanel));

        public MouseButton PanMouseButton
        {
            get => (MouseButton)GetValue(PanMouseButtonProperty);
            set => SetValue(PanMouseButtonProperty, value);
        }

        public Rect Viewport
        {
            get => (Rect)GetValue(ViewportProperty);
            set => SetValue(ViewportProperty, value);
        }

        public bool CanPan
        {
            get => (bool)GetValue(CanPanProperty);
            set => SetValue(CanPanProperty, value);
        }

        public bool CanZoom
        {
            get => (bool)GetValue(CanZoomProperty);
            set => SetValue(CanZoomProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (var uIElement in InternalChildren.OfType<UIElement>())
            {
                uIElement.Measure(uIElement is CartesianRenderer ? constraint : availableSize);
            }
            return default;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (invalidateTransform && !arrangeSize.IsEmpty)
            {
                var viewport = Viewport;
                Scale = new Vector(arrangeSize.Width / viewport.Width, arrangeSize.Height / viewport.Height);
                ZeroPosition = new Vector(-viewport.Left * Scale.X, viewport.Bottom * Scale.Y);
                invalidateTransform = false;
            }
            foreach (UIElement? uIElement in InternalChildren)
            {
                if (uIElement != null)
                {
                    if (uIElement is CartesianControl cartesianControl)
                    {
                        Point position = cartesianControl.Position;
                        cartesianControl.Arrange(new Rect(ModelToRender(position), cartesianControl.DesiredSize));
                    }
                    else if (uIElement is CartesianRenderer)
                    {
                        uIElement.Arrange(new Rect(arrangeSize));
                    }
                }
            }
            return arrangeSize;
        }

        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            if (ClipToBounds)
            {
                return new RectangleGeometry(new Rect(RenderSize));
            }
            return null;
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

        public Vector ZeroPosition { get; private set; }
        Point previousPanPosition;
        public Vector Scale { get; private set; } = new Vector(1, 1);
        const double ScaleSpeed = 1.05 / 120;
        private bool invalidateTransform;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (CanPan && GetMouseButtonState(e, PanMouseButton) == MouseButtonState.Pressed)
            {
                previousPanPosition = e.GetPosition(this);
                CaptureMouse();
                e.Handled = true;
            }
            base.OnPreviewMouseDown(e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured && GetMouseButtonState(e, PanMouseButton) != MouseButtonState.Pressed)
            {
                ReleaseMouseCapture();
                e.Handled = true;
            }
            base.OnPreviewMouseUp(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured && GetMouseButtonState(e, PanMouseButton) == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(this);
                var diff = currentPosition - previousPanPosition;
                if (diff.Length > 0)
                {
                    ZeroPosition += diff;
                    previousPanPosition = currentPosition;
                    InvalidateTransform();
                }
                e.Handled = true;
            }
            base.OnPreviewMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (CanZoom)
            {
                var pos = e.GetPosition(this);
                var scaleRatio = Math.Abs(e.Delta * ScaleSpeed);
                if (e.Delta < 0)
                    scaleRatio = 1 / scaleRatio;
                var vScaleRatio = new Vector(scaleRatio, scaleRatio);
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    vScaleRatio.Y = 1;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    vScaleRatio.X = 1;
                ZeroPosition = pos - (pos - ZeroPosition).ComponentMultiply(vScaleRatio);
                Scale = Scale.ComponentMultiply(vScaleRatio);
                InvalidateTransform();

                e.Handled = true;
            }
            base.OnMouseWheel(e);
        }

        public Point ModelToRender(Point modelPoint)
        {
            modelPoint.Y *= -1;
            return ZeroPosition + modelPoint.ComponentMultiply(Scale);
        }

        public Point RenderToModel(Point renderPoint)
        {
            var modelPoint = (renderPoint - ZeroPosition).ComponentDivide(Scale);
            modelPoint.Y *= -1;
            return modelPoint;
        }

        public void AutoScale()
        {
            var axis = InternalChildren.OfType<CartesianAxisLabels>().Single();
            var knotPositions = InternalChildren.OfType<CartesianAnimationCurveKnot>().Select(x => x.Position).ToList();

            double maxKnotX;
            double maxKnotY;
            double minKnotX;
            double minKnotY;

            if (knotPositions.Count == 0)
            {
                return;
            }
            else if (knotPositions.Count == 1)
            {
                maxKnotX = knotPositions[0].X + 1;
                maxKnotY = knotPositions[0].Y + 1;
                minKnotX = knotPositions[0].X - 1;
                minKnotY = knotPositions[0].Y - 1;
            }
            else
            {
                maxKnotX = knotPositions.Max(position => position.X);
                maxKnotY = knotPositions.Max(position => position.Y);
                minKnotX = knotPositions.Min(position => position.X);
                minKnotY = knotPositions.Min(position => position.Y);
            }

            maxKnotX += (maxKnotX - minKnotX) * 0.05;
            maxKnotY += (maxKnotY - minKnotY) * 0.05;
            minKnotX -= (maxKnotX - minKnotX) * 0.05;
            minKnotY -= (maxKnotY - minKnotY) * 0.05;

            var xScale = axis.RangeX / (maxKnotX - minKnotX);
            var yScale = axis.RangeY / (maxKnotY - minKnotY);
            Scale = Scale.ComponentMultiply(new Vector(xScale, yScale));

            ZeroPosition = new Vector(-minKnotX * ActualWidth * xScale / axis.RangeX, maxKnotY * ActualHeight * yScale / axis.RangeY);
            InvalidateTransform();
        }

        private void InvalidateTransform()
        {
            InvalidateArrange();
            InvalidateVisual();
            foreach (var uielement in Children)
            {
                if (uielement is CartesianRenderer cr)
                    cr.InvalidateVisual();
            }
            foreach (var uielement in VisualTreeHelperEx.GetDescendants<ICartesianVisual>(this))
            {
                uielement.InvalidateTransform();
            }
        }

        private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianPanel panel = (CartesianPanel)d;
            panel.OnViewportChanged();
        }

        private void OnViewportChanged()
        {
            invalidateTransform = true;
            InvalidateTransform();
        }
    }
}
