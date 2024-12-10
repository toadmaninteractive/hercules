using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using JsonDiff;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace Hercules.Controls
{
    public class DiffHighlightBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty DiffLinesProperty = DependencyProperty.Register("DiffLines", typeof(IDiffLines), typeof(DiffHighlightBehavior), new PropertyMetadata(null, OnDiffLinesPropertyChanged));
        public static readonly DependencyProperty DiffMarkerProperty = DependencyProperty.Register("DiffMarker", typeof(bool), typeof(DiffHighlightBehavior), new PropertyMetadata(false, OnDiffMarkerPropertyChanged));

        public IDiffLines? DiffLines
        {
            get => (IDiffLines)GetValue(DiffLinesProperty);
            set => SetValue(DiffLinesProperty, value);
        }

        public bool DiffMarker
        {
            get => (bool)GetValue(DiffMarkerProperty);
            set => SetValue(DiffMarkerProperty, value);
        }

        private static void OnDiffLinesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DiffHighlightBehavior)d).DiffLinesChanged(e);
        }

        private static void OnDiffMarkerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DiffHighlightBehavior)d).DiffMarkerChanged(e);
        }

        DiffLineBackgroundRenderer? renderer;
        DiffMarkerMargin? margin;

        protected void DiffLinesChanged(DependencyPropertyChangedEventArgs e)
        {
            if (renderer != null)
                renderer.Lines = e.NewValue as IDiffLines;
            if (margin != null)
                margin.DiffLines = e.NewValue as IDiffLines;
        }

        protected void DiffMarkerChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                InstallMargin();
            else
                UninstallMargin();
        }

        protected override void OnAttached()
        {
            renderer = new DiffLineBackgroundRenderer(DiffLines);
            AssociatedObject.TextArea.TextView.BackgroundRenderers.Add(renderer);
            if (DiffMarker)
                InstallMargin();
        }

        protected override void OnDetaching()
        {
            UninstallMargin();
            AssociatedObject.TextArea.TextView.BackgroundRenderers.Remove(renderer);
            renderer = null;
        }

        void InstallMargin()
        {
            if (margin != null || renderer == null)
                return;
            margin = new DiffMarkerMargin();
            AssociatedObject.TextArea.LeftMargins.Insert(0, margin);
        }

        void UninstallMargin()
        {
            if (margin == null)
                return;
            AssociatedObject.TextArea.LeftMargins.Remove(margin);
            margin = null;
        }
    }
}
