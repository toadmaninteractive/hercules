using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hercules
{
    public interface IControlPool
    {
        FrameworkElement Acquire();
        void Bind(FrameworkElement element, object? content);
        void Release(FrameworkElement control);
    }

    public abstract class ControlPool : IControlPool
    {
        private readonly Stack<FrameworkElement> pool = new();

        public int Total { get; private set; }
        public int Available => pool.Count;

        public FrameworkElement Acquire()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }
            else
            {
                Total++;
                return CreateInstance();
            }
        }

        protected abstract FrameworkElement CreateInstance();

        public abstract void Bind(FrameworkElement element, object? content);

        public void Release(FrameworkElement control)
        {
            pool.Push(control);
        }
    }

    public class ContentPresenterPool : ControlPool
    {
        private readonly ResourceDictionary resourceDictionary;
        private readonly object dataTemplateKey;

        public ContentPresenterPool(ResourceDictionary resourceDictionary, object dataTemplateKey)
        {
            this.resourceDictionary = resourceDictionary;
            this.dataTemplateKey = dataTemplateKey;
        }

        protected override FrameworkElement CreateInstance()
        {
            var dataTemplate = resourceDictionary[dataTemplateKey] as DataTemplate;
            if (dataTemplate == null)
                throw new InvalidOperationException($"DataTemplate {dataTemplateKey} not found");
            return new ContentPresenter { ContentTemplate = dataTemplate };
        }

        public override void Bind(FrameworkElement element, object? content)
        {
            ((ContentPresenter)element).Content = content;
        }

        public override string ToString()
        {
            return $"{GetType()}: {dataTemplateKey}";
        }
    }

    public class FactoryControlPool : ControlPool
    {
        private readonly Func<FrameworkElement> factory;
        private readonly string tag;

        public FactoryControlPool(Func<FrameworkElement> factory, string tag)
        {
            this.factory = factory;
            this.tag = tag;
        }

        protected override FrameworkElement CreateInstance()
        {
            var result = factory();
            result.Tag = tag;
            return result;
        }

        public override void Bind(FrameworkElement element, object? content)
        {
        }

        public override string ToString()
        {
            return $"{GetType()}: {tag}";
        }
    }

    public static class ControlPools
    {
        static readonly Dictionary<object, IControlPool> pools = new();
        private static readonly Lazy<ResourceDictionary> resourceDictionary = new(LoadResourceDictionary);

        private static ResourceDictionary LoadResourceDictionary()
        {
            return new ResourceDictionary { Source = new Uri("pack://application:,,,/Hercules;component/Forms/Style/Elements/Elements.xaml", UriKind.RelativeOrAbsolute) };
        }

        public static IControlPool GetPool(Type key)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new ContentPresenterPool(resourceDictionary.Value, new DataTemplateKey(key));
                pools.Add(key, pool);
            }
            return pool;
        }

        public static IControlPool GetPool(string key)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new ContentPresenterPool(resourceDictionary.Value, key);
                pools.Add(key, pool);
            }
            return pool;
        }

        public static void Report()
        {
            foreach (var pair in pools)
            {
                var pool = (ControlPool)pair.Value;
                Logger.LogDebug($"Pool of {pair.Key}: {pool.Available}/{pool.Total}");
            }
        }
    }
}
