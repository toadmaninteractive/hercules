using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class CartesianAnimationCurveKnot : CartesianControl
    {
        public static readonly DependencyProperty TangentInProperty = DependencyProperty.Register("TangentIn", typeof(double), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata((double)0, OnTangentChanged));
        public static readonly DependencyProperty TangentOutProperty = DependencyProperty.Register("TangentOut", typeof(double), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata((double)0, OnTangentChanged));
        public static readonly DependencyProperty SingleClickCommandProperty = DependencyProperty.Register("SingleClickCommand", typeof(ICommand), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty SingleRightClickCommandProperty = DependencyProperty.Register("SingleRightClickCommand", typeof(ICommand), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty SingleControlClickCommandProperty = DependencyProperty.Register("SingleControlClickCommand", typeof(ICommand), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(CartesianAnimationCurveKnot), new FrameworkPropertyMetadata(null));

        public ICommand SingleClickCommand
        {
            get => (ICommand)GetValue(SingleClickCommandProperty);
            set => SetValue(SingleClickCommandProperty, value);
        }

        public ICommand SingleControlClickCommand
        {
            get => (ICommand)GetValue(SingleControlClickCommandProperty);
            set => SetValue(SingleControlClickCommandProperty, value);
        }

        public ICommand SingleRightClickCommand
        {
            get => (ICommand)GetValue(SingleRightClickCommandProperty);
            set => SetValue(SingleRightClickCommandProperty, value);
        }

        public ICommand DoubleClickCommand
        {
            get => (ICommand)GetValue(DoubleClickCommandProperty);
            set => SetValue(DoubleClickCommandProperty, value);
        }

        public double TangentIn
        {
            get => (double)GetValue(TangentInProperty);
            set => SetValue(TangentInProperty, value);
        }

        public double TangentOut
        {
            get => (double)GetValue(TangentOutProperty);
            set => SetValue(TangentOutProperty, value);
        }

        private static void OnTangentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CartesianAnimationCurveKnot control)
            {
                control.Curve.InvalidateVisual();
                control.InvalidateVisual();
            }
        }

        public CartesianAnimationCurve Curve { get; }

        public CartesianAnimationCurveKnot(CartesianAnimationCurve curve)
        {
            Curve = curve;
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            Curve.InvalidateVisual();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            ICommand? command = null;
            if (e.ClickCount == 1)
            {
                command = Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                    ? SingleControlClickCommand
                    : SingleClickCommand;
            }
            else if (e.ClickCount == 2)
                command = DoubleClickCommand;

            if (command != null && e.Source is CartesianAnimationCurveKnot knot)
            {
                var knotModel = knot.DataContext;
                if (command.CanExecute(knotModel))
                    command.Execute(knotModel);
            }
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && e.Source is CartesianAnimationCurveKnot knot && SingleRightClickCommand != null)
            {
                var knotModel = knot.DataContext;
                if (SingleRightClickCommand.CanExecute(knotModel))
                    SingleRightClickCommand.Execute(knotModel);
            }
            base.OnPreviewMouseRightButtonDown(e);
        }
    }
}
