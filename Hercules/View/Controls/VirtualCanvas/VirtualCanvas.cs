using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hercules.Controls
{
    public interface IVirtualRow
    {
        double Top { get; }
        double Height { get; }
        IVirtualRow? Next { get; }
        int Version { get; }
        bool Pinned { get; }
        UIElement? Visual { get; }
        UIElement CreateVisual(VirtualCanvas parent);
        void DisposeVisual();
    }

    /// <summary>
    /// VirtualCanvas dynamically figures out which rows are visible and creates their visuals 
    /// and which rows are no longer visible (due to scrolling or zooming) and destroys their
    /// visuals.  This helps manage the memory consumption when you have so many objects that creating
    /// all the WPF visuals would take too much memory.
    /// </summary>
    public class VirtualCanvas : VirtualizingPanel, IScrollInfo
    {
        /// <summary>
        /// The ContentCanvas that is actually the parent of all the VirtualChildren Visuals.
        /// </summary>
        public Canvas ContentCanvas { get; }

        /// <summary>
        /// Return the full size of this canvas.
        /// </summary>
        public Size Extent => extent;

        public IVirtualRow? FirstRow { get; private set; }

        /// <summary>
        /// Returns true if all Visuals have been created for the current scroll position
        /// and there is no more idle processing needed.
        /// </summary>
        public bool IsDone { get; private set; } = true;

        public ScaleTransform Scale { get; }

        public Size SmallScrollIncrement { get; set; } = new Size(25, 25);

        public TranslateTransform Translate { get; }

        public const double WidthTemp = 1000;
        Size extent;
        readonly VirtualCanvasIndex index;
        Size viewPortSize;
        private readonly List<IVirtualRow> clearRows = new();
        private readonly HashSet<IVirtualRow> visibleRows = new();

        private readonly PanGesture panGesture;

        public VirtualCanvas()
        {
            this.Background = Brushes.Transparent;
            index = new VirtualCanvasIndex();
            Scale = new ScaleTransform();
            Scale.Changed += OnScaleChanged;
            Translate = new TranslateTransform();
            Translate.Changed += OnTranslateChanged;
            ContentCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                RenderTransform = new TransformGroup { Children = { Scale, Translate } }
            };
            this.Children.Add(ContentCanvas);

            panGesture = new PanGesture(this);
        }

        public void EnsureVisual(IVirtualRow row)
        {
            if (row.Visual != null)
                return;

            UIElement e = row.CreateVisual(this);
            visibleRows.Add(row);
            Canvas.SetTop(e, row.Top);

            ContentCanvas.Children.Add(e);
        }

        /// <summary>
        /// Tell the ScrollViewer to update the scrollbars because, extent, zoom or translate has changed.
        /// </summary>
        public void InvalidateScrollInfo()
        {
            ScrollOwner?.InvalidateScrollInfo();
        }

        public void RefreshVisuals(IVirtualRow? firstRow)
        {
            FirstRow = firstRow;
            int version = firstRow?.Version ?? -1;
            index.Clear();
            CalculateExtent();
            // need to rebuild the index.
            IsDone = false;
            foreach (var row in visibleRows)
            {
                if (row.Version != version)
                {
                    clearRows.Add(row);
                }
                else
                {
                    Canvas.SetTop(row.Visual, row.Top);
                    row.Visual!.SetValue(HeightProperty, row.Height);
                }
            }
            foreach (IVirtualRow row in clearRows)
            {
                ContentCanvas.Children.Remove(row.Visual);
                row.DisposeVisual();
            }
            visibleRows.ExceptWith(clearRows);
            clearRows.Clear();
            InvalidateArrange();
            StartLazyUpdate();
            if (VerticalOffset >= index.Height)
                SetVerticalOffset(Math.Max(index.Height - ViewportHeight, 0));
        }

        public void ScrollIntoView(IVirtualRow row, IVirtualRow? toRow)
        {
            if (toRow == null)
            {
                if (VerticalOffset >= row.Top || VerticalOffset + ActualHeight - 20 <= row.Top)
                    SetVerticalOffset(row.Top);
            }
            else
            {
                if (VerticalOffset >= row.Top)
                    SetVerticalOffset(row.Top);
                else if (VerticalOffset + ActualHeight - 20 <= toRow.Top)
                    SetVerticalOffset(Math.Max(row.Top, VerticalOffset + ActualHeight - 20 - (toRow.Top - row.Top)));
            }
            EnsureVisual(row);
        }

        /// <summary>
        /// WPF ArrangeOverride for laying out the control
        /// </summary>
        /// <param name="finalSize">The size allocated by parents</param>
        /// <returns>finalSize</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(finalSize);

            CalculateExtent();

            if (finalSize != viewPortSize)
            {
                SetViewportSize(finalSize);
            }

            ContentCanvas.Arrange(new Rect(0, 0, ContentCanvas.Width, ContentCanvas.Height));
            return finalSize;
        }

        /// <summary>
        /// WPF Measure override for measuring the control
        /// </summary>
        /// <param name="availableSize">Available size will be the viewport size in the scroll viewer</param>
        /// <returns>availableSize</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);

            // We will be given the visible size in the scroll viewer here.
            CalculateExtent();

            if (availableSize != viewPortSize)
            {
                SetViewportSize(availableSize);
            }

            foreach (UIElement child in this.InternalChildren)
            {
                child.Measure(new Size(WidthTemp, (double)child.GetValue(HeightProperty)));
            }
            if (double.IsInfinity(availableSize.Width))
            {
                return extent;
            }
            else
            {
                return availableSize;
            }
        }

        /// <summary>
        /// Calculate the size needed to display all the virtual children.
        /// </summary>
        void CalculateExtent()
        {
            if (!index.IsInitialized || extent.Width == 0 || extent.Height == 0 || double.IsNaN(extent.Width) || double.IsNaN(extent.Height))
            {
                index.Rebuild(FirstRow);
                extent = new Size(WidthTemp, index.Height);
            }

            // Make sure we honor the min width & height.
            double w = Math.Max(ContentCanvas.MinWidth, extent.Width);
            double h = Math.Max(ContentCanvas.MinHeight, extent.Height);
            ContentCanvas.Width = w;
            ContentCanvas.Height = h;

            // Make sure the backdrop covers the ViewPort bounds.
            double zoom = Scale.ScaleX;
            if (!double.IsInfinity(this.ViewportHeight) &&
                !double.IsInfinity(this.ViewportHeight))
            {
                w = Math.Max(w, this.ViewportWidth / zoom);
                h = Math.Max(h, this.ViewportHeight / zoom);
            }

            ScrollOwner?.InvalidateScrollInfo();
        }

        /// <summary>
        /// Get the currently visible range according to current scroll position and zoom factor and
        /// size of scroller viewport.
        /// </summary>
        /// <returns>A rectangle</returns>
        Range GetVisibleRange()
        {
            // Add a bit of extra around the edges so we are sure to create nodes that have a tiny bit showing.
            double ystart = (this.VerticalOffset - SmallScrollIncrement.Height) / Scale.ScaleY;
            double yend = (this.VerticalOffset + (viewPortSize.Height + (2 * SmallScrollIncrement.Height))) / Scale.ScaleY;
            return new Range(ystart, yend - ystart);
        }

        private int LazyCreateRows(int quantum, Range visible)
        {
            int count = 0;

            // Iterate over the visible range of nodes and make sure they have visuals.
            foreach (IVirtualRow row in index.GetRowsInside(visible))
            {
                if (row.Visual == null)
                {
                    EnsureVisual(row);
                    count++;
                }

                if (count >= quantum)
                {
                    IsDone = false;
                    break;
                }
            }
            return count;
        }

        private int LazyRemoveRows(int quantum, Range visible)
        {
            if (quantum <= 0)
            {
                IsDone = false;
                return 0;
            }
            int count = 0;

            foreach (var row in visibleRows)
            {
                if (row.Pinned)
                    continue;
                Range nrect = new Range(row.Top, row.Height);
                if (!nrect.IntersectsWith(visible))
                {
                    clearRows.Add(row);
                    count++;
                    if (count >= quantum)
                    {
                        IsDone = false;
                        break;
                    }
                }
            }

            foreach (IVirtualRow row in clearRows)
            {
                ContentCanvas.Children.Remove(row.Visual);
                row.DisposeVisual();
            }

            visibleRows.ExceptWith(clearRows);
            clearRows.Clear();
            return count;
        }

        /// <summary>
        /// Do a quantized unit of work for creating newly visible visuals, and cleaning up visuals that are no
        /// longer needed.
        /// </summary>
        void LazyUpdateVisuals()
        {
            const int _createQuanta = 10;
            const int _removeQuanta = 10;

            IsDone = true;

            Range visible = GetVisibleRange();
            int added = LazyCreateRows(_createQuanta, visible);
            LazyRemoveRows(_removeQuanta, visible);

            if (added > 0)
            {
                InvalidateArrange();
            }
            if (!IsDone)
            {
                StartLazyUpdate();
            }
            this.InvalidateVisual();
        }

        /// <summary>
        /// Callback whenever the current ScaleTransform is changed.
        /// </summary>
        /// <param name="sender">ScaleTransform</param>
        /// <param name="e">noop</param>
        void OnScaleChanged(object? sender, EventArgs e)
        {
            OnScrollChanged();
        }

        /// <summary>
        /// The visible region has changed, so we need to queue up work for dirty regions and new visible regions
        /// then start asynchronously building new visuals via StartLazyUpdate.
        /// </summary>
        void OnScrollChanged()
        {
            IsDone = false;

            StartLazyUpdate();
            InvalidateScrollInfo();
        }

        /// <summary>
        /// Callback whenever the current TranslateTransform is changed.
        /// </summary>
        /// <param name="sender">TranslateTransform</param>
        /// <param name="e">noop</param>
        void OnTranslateChanged(object? sender, EventArgs e)
        {
            OnScrollChanged();
        }

        /// <summary>
        /// Set the viewport size and raize a scroll changed event.
        /// </summary>
        /// <param name="s">The new size</param>
        void SetViewportSize(Size s)
        {
            if (s != viewPortSize)
            {
                viewPortSize = s;
                OnScrollChanged();
            }
        }

        void StartLazyUpdate()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, LazyUpdateVisuals);
        }

        #region IScrollInfo Members

        /// <summary>
        /// Return whether we are allowed to scroll horizontally.
        /// </summary>
        public bool CanHorizontallyScroll { get; set; } = false;

        /// <summary>
        /// Return whether we are allowed to scroll vertically.
        /// </summary>
        public bool CanVerticallyScroll { get; set; } = false;

        /// <summary>
        /// The height of the canvas to be scrolled.
        /// </summary>
        public double ExtentHeight => extent.Height * Scale.ScaleY;

        /// <summary>
        /// The width of the canvas to be scrolled.
        /// </summary>
        public double ExtentWidth => extent.Width * Scale.ScaleX;

        /// <summary>
        /// Get the current horizontal scroll position.
        /// </summary>
        public double HorizontalOffset => -Translate.X;

        /// <summary>
        /// Return the ScrollViewer that contains this object.
        /// </summary>
        public ScrollViewer? ScrollOwner { get; set; }

        /// <summary>
        /// Return the current vertical scroll position.
        /// </summary>
        public double VerticalOffset => -Translate.Y;

        /// <summary>
        /// Return the height of the current viewport that is visible in the ScrollViewer.
        /// </summary>
        public double ViewportHeight => viewPortSize.Height;

        /// <summary>
        /// Return the width of the current viewport that is visible in the ScrollViewer.
        /// </summary>
        public double ViewportWidth => viewPortSize.Width;

        /// <summary>
        /// Scroll down one small scroll increment.
        /// </summary>
        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + (SmallScrollIncrement.Height * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll left by one small scroll increment.
        /// </summary>
        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - (SmallScrollIncrement.Width * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll right by one small scroll increment
        /// </summary>
        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + (SmallScrollIncrement.Width * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll up by one small scroll increment
        /// </summary>
        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - (SmallScrollIncrement.Height * Scale.ScaleX));
        }

        /// <summary>
        /// Make the given visual at the given bounds visible.
        /// </summary>
        /// <param name="visual">The visual that will become visible</param>
        /// <param name="rectangle">The bounds of that visual</param>
        /// <returns>The bounds that is actually visible.</returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        /// <summary>
        /// Scroll down by one mouse wheel increment.
        /// </summary>
        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + (SmallScrollIncrement.Height * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll left by one mouse wheel increment.
        /// </summary>
        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset + (SmallScrollIncrement.Width * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll right by one mouse wheel increment.
        /// </summary>
        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset - (SmallScrollIncrement.Width * Scale.ScaleX));
        }

        /// <summary>
        /// Scroll up by one mouse wheel increment.
        /// </summary>
        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - (SmallScrollIncrement.Height * Scale.ScaleX));
        }

        /// <summary>
        /// Page down by one view port height amount.
        /// </summary>
        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + viewPortSize.Height);
        }

        /// <summary>
        /// Page left by one view port width amount.
        /// </summary>
        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - viewPortSize.Width);
        }

        /// <summary>
        /// Page right by one view port width amount.
        /// </summary>
        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + viewPortSize.Width);
        }

        /// <summary>
        /// Page up by one view port height amount.
        /// </summary>
        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - viewPortSize.Height);
        }
        /// <summary>
        /// Scroll to the given absolute horizontal scroll position.
        /// </summary>
        /// <param name="offset">The horizontal position to scroll to</param>
        public void SetHorizontalOffset(double offset)
        {
            double xoffset = Math.Max(Math.Min(offset, ExtentWidth - ViewportWidth), 0);
            Translate.X = -xoffset;
            OnScrollChanged();
        }

        /// <summary>
        /// Scroll to the given absolute vertical scroll position.
        /// </summary>
        /// <param name="offset">The vertical position to scroll to</param>
        public void SetVerticalOffset(double offset)
        {
            double yoffset = Math.Max(Math.Min(offset, ExtentHeight - ViewportHeight), 0);
            Translate.Y = -yoffset;
            OnScrollChanged();
        }
        #endregion

        public void ScrollBy(double offset)
        {
            double yoffset = Math.Max(Math.Min(-Translate.Y - offset, ExtentHeight - ViewportHeight), 0);
            Translate.Y = -yoffset;
            OnScrollChanged();
        }
    }
}
