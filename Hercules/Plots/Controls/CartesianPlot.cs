using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class CartesianPlot : CartesianRenderer
    {
        protected class KnotElement
        {
            public object ViewModel { get; }
            public CartesianControl View { get; }

            public KnotElement(object viewModel, CartesianControl view)
            {
                View = view;
                ViewModel = viewModel;
            }
        }

        public static readonly DependencyProperty KnotsProperty = DependencyProperty.Register("Knots", typeof(IList), typeof(CartesianPlot), new FrameworkPropertyMetadata(null, OnKnotsChanged));

        public static readonly DependencyProperty KnotStyleProperty = DependencyProperty.Register("KnotStyle", typeof(Style), typeof(CartesianPlot), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(CartesianPlot), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, OnPenChanged));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CartesianPlot), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnPenChanged));

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

        public IList? Knots
        {
            get => (IList)GetValue(KnotsProperty);
            set => SetValue(KnotsProperty, value);
        }

        public Style KnotStyle
        {
            get => (Style)GetValue(KnotStyleProperty);
            set => SetValue(KnotStyleProperty, value);
        }

        private static void OnKnotsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CartesianPlot)d).OnKnotsChanged((IList)e.NewValue);
        }

        private IList? knots;
        private Pen? pen;

        private static void OnPenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CartesianPlot)d).pen = null;
        }

        protected Pen GetStrokePen()
        {
            if (pen == null)
                pen = new Pen(Stroke, StrokeThickness);
            return pen;
        }

        protected List<KnotElement> Elements { get; } = new List<KnotElement>();

        private void OnKnotsChanged(IList newKnots)
        {
            if (knots != null)
            {
                if (knots is INotifyCollectionChanged notifyCollectionChanged)
                    notifyCollectionChanged.CollectionChanged -= Knots_CollectionChanged;
                foreach (var element in Elements)
                {
                    var view = element.View;
                    // Fix for "Cannot modify the logical children for this node at this time because a tree walk is in progress" error
                    Dispatcher.BeginInvoke(new Action(() => Panel.Children.Remove(view)), System.Windows.Threading.DispatcherPriority.Background);
                }
                Elements.Clear();
            }
            knots = newKnots;
            if (knots != null)
            {
                if (knots is INotifyCollectionChanged notifyCollectionChanged)
                    notifyCollectionChanged.CollectionChanged += Knots_CollectionChanged;
                foreach (var knot in knots)
                {
                    OnAddKnot(knot!);
                }
            }
            InvalidateVisual();
        }

        private void OnAddKnot(object viewModel)
        {
            var view = CreateKnotView(viewModel, KnotStyle);
            var element = new KnotElement(viewModel, view);
            Elements.Add(element);
            // Fix for "Cannot modify the logical children for this node at this time because a tree walk is in progress" error
            Dispatcher.BeginInvoke(new Action(() => Panel.Children.Add(view)), System.Windows.Threading.DispatcherPriority.Background);
        }

        protected virtual CartesianControl CreateKnotView(object viewModel, Style knotStyle)
        {
            return new CartesianControl { DataContext = viewModel, Style = knotStyle };
        }

        private void OnRemoveKnot(object viewModel)
        {
            var element = Elements.Find(e => e.ViewModel == viewModel)!;
            Panel.Children.Remove(element.View);
            Elements.Remove(element);
        }

        private void Knots_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems!)
                        OnAddKnot(item!);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems!)
                        OnRemoveKnot(item!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var element in Elements)
                    {
                        Panel.Children.Remove(element.View);
                    }
                    Elements.Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems!)
                        OnRemoveKnot(item!);
                    foreach (var item in e.NewItems!)
                        OnAddKnot(item!);
                    break;
            }
            InvalidateVisual();
        }
    }
}
