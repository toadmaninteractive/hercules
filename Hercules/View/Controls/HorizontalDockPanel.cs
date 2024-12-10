using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hercules.Controls
{
    public enum HorizontalDock
    {
        Left,
        Right,
        Flexible,
        Fill,
    }

    public class HorizontalDockPanel : Panel
    {
        public static readonly DependencyProperty HorizontalDockProperty = DependencyProperty.RegisterAttached(
            "Dock", typeof(HorizontalDock), typeof(HorizontalDockPanel),
            new FrameworkPropertyMetadata(HorizontalDock.Left, OnHorizontalDockChanged),
            IsValidHorizontalDock);

        public static HorizontalDock GetDock(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (HorizontalDock)element.GetValue(HorizontalDockProperty);
        }

        public static void SetDock(UIElement element, HorizontalDock dock)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(HorizontalDockProperty, dock);
        }

        private static void OnHorizontalDockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement uie && VisualTreeHelper.GetParent(uie) is HorizontalDockPanel p)
            {
                p.InvalidateMeasure();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;
            int totalChildrenCount = children.Count;

            const double preferredFillWidth = 320;

            double fixedWidth = 0;
            double flexibleWidth = 0;
            double fullWidth = constraint.Width;
            if (double.IsInfinity(fullWidth))
                fullWidth = 1000.0;
            int fillCount = 0;
            int flexCount = 0;

            for (int i = 0; i < totalChildrenCount; ++i)
            {
                var child = children[i] as FrameworkElement;
                if (child == null)
                    continue;

                fixedWidth += child.Margin.Left;

                switch (GetDock(child))
                {
                    case HorizontalDock.Left:
                    case HorizontalDock.Right:
                        child.Measure(constraint);
                        fixedWidth += child.DesiredSize.Width;
                        break;

                    case HorizontalDock.Flexible:
                        flexibleWidth += child.MaxWidth - child.MinWidth;
                        fixedWidth += child.MinWidth;
                        flexCount++;
                        break;

                    case HorizontalDock.Fill:
                        fixedWidth += child.MinWidth;
                        flexibleWidth += preferredFillWidth - child.MinWidth;
                        flexCount++;
                        fillCount++;
                        break;
                }
            }

            double extraFillWidth = 0;
            double flexibleRatio = 1;
            if (flexCount == 0)
            {
            }
            else if (fixedWidth > fullWidth)
            {
                flexibleRatio = 0;
            }
            else if (fullWidth > fixedWidth + flexibleWidth)
            {
                if (fillCount > 0)
                {
                    extraFillWidth = (fullWidth - flexibleWidth - fixedWidth) / fillCount;
                }
            }
            else
            {
                flexibleRatio = Math.Max(fullWidth - fixedWidth, 0) / flexibleWidth;
            }

            double totalWidth = 0;
            for (int i = 0; i < totalChildrenCount; ++i)
            {
                var child = children[i] as FrameworkElement;
                if (child == null)
                    continue;

                switch (GetDock(child))
                {
                    case HorizontalDock.Left:
                    case HorizontalDock.Right:
                        totalWidth += child.DesiredSize.Width;
                        break;

                    case HorizontalDock.Flexible:
                        double flexWidth = child.MinWidth + (child.MaxWidth - child.MinWidth) * flexibleRatio;
                        child.Measure(new Size(flexWidth, constraint.Height));
                        totalWidth += flexWidth;
                        break;

                    case HorizontalDock.Fill:
                        double fillWidth = child.MinWidth + (preferredFillWidth - child.MinWidth) * flexibleRatio + extraFillWidth;
                        child.Measure(new Size(fillWidth, constraint.Height));
                        totalWidth += fillWidth;
                        break;
                }
            }

            return new Size(totalWidth, constraint.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            int totalChildrenCount = children.Count;

            const double preferredFillWidth = 320;

            double fixedWidth = 0;
            double flexibleWidth = 0;
            double fullWidth = arrangeSize.Width;
            if (double.IsInfinity(fullWidth))
                fullWidth = 1000.0;
            int fillCount = 0;
            int flexCount = 0;

            for (int i = 0; i < totalChildrenCount; ++i)
            {
                var child = children[i] as FrameworkElement;
                if (child == null)
                    continue;

                fixedWidth += child.Margin.Left;

                switch (GetDock(child))
                {
                    case HorizontalDock.Left:
                    case HorizontalDock.Right:
                        fixedWidth += child.DesiredSize.Width;
                        break;

                    case HorizontalDock.Flexible:
                        flexibleWidth += child.MaxWidth - child.MinWidth;
                        fixedWidth += child.MinWidth;
                        flexCount++;
                        break;

                    case HorizontalDock.Fill:
                        fixedWidth += child.MinWidth;
                        flexibleWidth += preferredFillWidth - child.MinWidth;
                        flexCount++;
                        fillCount++;
                        break;
                }
            }

            double extraFillWidth = 0;
            double flexibleRatio = 1;
            if (flexCount == 0)
            {
            }
            else if (fixedWidth > fullWidth)
            {
                flexibleRatio = 0;
            }
            else if (fullWidth > fixedWidth + flexibleWidth)
            {
                if (fillCount > 0)
                {
                    extraFillWidth = (fullWidth - flexibleWidth - fixedWidth) / fillCount;
                }
            }
            else
            {
                flexibleRatio = Math.Max(fullWidth - fixedWidth, 0) / flexibleWidth;
            }
            Debug.Assert(!double.IsNaN(extraFillWidth) && !double.IsInfinity(extraFillWidth));
            Debug.Assert(!double.IsNaN(flexibleRatio) && !double.IsInfinity(flexibleRatio));

            double accumulatedLeft = 0;
            double accumulatedRight = 0;
            for (int i = 0; i < totalChildrenCount; ++i)
            {
                var child = children[i] as FrameworkElement;
                if (child == null)
                    continue;

                Rect rcChild = new Rect(accumulatedLeft, 0, 0, child.DesiredSize.Height);

                switch (GetDock(child))
                {
                    case HorizontalDock.Left:
                        rcChild.Width = child.DesiredSize.Width;
                        accumulatedLeft += rcChild.Width;
                        break;

                    case HorizontalDock.Right:
                        accumulatedRight += child.DesiredSize.Width;
                        rcChild.X = Math.Max(0.0, arrangeSize.Width - accumulatedRight);
                        rcChild.Width = child.Width;
                        break;

                    case HorizontalDock.Flexible:
                        rcChild.Width = child.MinWidth + (child.MaxWidth - child.MinWidth) * flexibleRatio;
                        accumulatedLeft += rcChild.Width;
                        break;

                    case HorizontalDock.Fill:
                        rcChild.Width = child.MinWidth + (preferredFillWidth - child.MinWidth) * flexibleRatio + extraFillWidth;
                        accumulatedLeft += rcChild.Width;
                        break;
                }

                child.Arrange(rcChild);
            }

            return arrangeSize;
        }

        private static bool IsValidHorizontalDock(object o)
        {
            HorizontalDock dock = (HorizontalDock)o;
            return (dock == HorizontalDock.Left || dock == HorizontalDock.Right || dock == HorizontalDock.Fill || dock == HorizontalDock.Flexible);
        }
    }
}
