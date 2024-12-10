using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Controls
{
    public class CacheViewBehavior : Behavior<ContentControl>
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(CacheViewBehavior), new PropertyMetadata((obj, args) =>
        {
            ((CacheViewBehavior)obj).ContentChanged(args.NewValue);
        }));

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        private Dictionary<Type, FrameworkElement>? cache;

        protected override void OnAttached()
        {
            cache = new Dictionary<Type, FrameworkElement>();
        }

        protected override void OnDetaching()
        {
            cache = null;
        }

        void ContentChanged(object content)
        {
            if (AssociatedObject.Content != null)
                ((FrameworkElement)AssociatedObject.Content).DataContext = null;

            if (content == null)
            {
                AssociatedObject.Content = null;
                return;
            }

            var contentType = content.GetType();
            if (!cache!.TryGetValue(contentType, out var view))
            {
                if (ViewModelTypes.TryGetViewTypeByViewModelType(content.GetType(), out var viewType))
                {
                    view = (FrameworkElement)Activator.CreateInstance(viewType)!;
                    cache[contentType] = view;
                }
            }

            if (view != null)
            {
                view.DataContext = content;
                AssociatedObject.Content = view;
            }
        }
    }
}
