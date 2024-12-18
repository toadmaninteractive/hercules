using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using JsonDiff;
using Microsoft.Xaml.Behaviors;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;

namespace Hercules.Controls
{
    public class DiffChunksBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty DiffChunksProperty = DependencyProperty.Register("DiffChunks", typeof(IDiffChunks), typeof(DiffChunksBehavior), new PropertyMetadata(null, OnDiffChunksPropertyChanged));

        public IDiffChunks DiffChunks
        {
            get => (IDiffChunks)GetValue(DiffChunksProperty);
            set => SetValue(DiffChunksProperty, value);
        }

        private static void OnDiffChunksPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DiffChunksBehavior)d).DiffChunksChanged(e);
        }

        DiffChunksMargin? margin;
        DiffChunksColorizer? colorizer;
        IDisposable? subscription;

        protected void DiffChunksChanged(DependencyPropertyChangedEventArgs e)
        {
            var newChunks = e.NewValue as IDiffChunks;
            if (margin != null)
                margin.DiffChunks = newChunks;
            if (colorizer != null)
                colorizer.Chunks = newChunks;

            if (subscription != null)
            {
                subscription.Dispose();
                subscription = null;
            }
            if (newChunks != null)
            {
                subscription = newChunks.WhenChanged.Throttle(TimeSpan.FromSeconds(0.1)).ObserveOn(DispatcherScheduler.Current).Subscribe(_ => Update());
            }
        }

        protected override void OnAttached()
        {
            InstallMargin();
        }

        protected override void OnDetaching()
        {
            UninstallMargin();
        }

        void Update()
        {
            margin?.InvalidateVisual();
            AssociatedObject.TextArea.TextView.Redraw();
        }

        void InstallMargin()
        {
            if (margin == null)
            {
                margin = new DiffChunksMargin();
                AssociatedObject.TextArea.LeftMargins.Insert(0, margin);
            }
            if (colorizer == null)
            {
                colorizer = new DiffChunksColorizer(AssociatedObject);
                AssociatedObject.TextArea.TextView.LineTransformers.Add(colorizer);
            }
        }

        void UninstallMargin()
        {
            if (margin != null)
            {
                AssociatedObject.TextArea.LeftMargins.Remove(margin);
                margin = null;
            }
            if (colorizer != null)
            {
                AssociatedObject.TextArea.TextView.LineTransformers.Remove(colorizer);
                colorizer = null;
            }
        }
    }
}
