// TabContent.cs, version 1.2
// The code in this file is Copyright (c) Ivan Krivyakov
// See http://www.ikriv.com/legal.php for more information

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Hercules.Controls
{
    /// <summary>
    /// Attached properties for persistent tab control
    /// </summary>
    /// <remarks>By default WPF TabControl bound to an ItemsSource destroys visual state of invisible tabs.
    /// Set ikriv:TabContent.IsCached="True" to preserve visual state of each tab.
    /// </remarks>
    public static class TabContent
    {
        public static bool GetIsCached(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(IsCachedProperty);
        }

        public static void SetIsCached(DependencyObject depObj, bool value)
        {
            depObj.SetValue(IsCachedProperty, value);
        }

        /// <summary>
        /// Controls whether tab content is cached or not
        /// </summary>
        /// <remarks>When TabContent.IsCached is true, visual state of each tab is preserved (cached), even when the tab is hidden</remarks>
        public static readonly DependencyProperty IsCachedProperty =
            DependencyProperty.RegisterAttached("IsCached", typeof(bool), typeof(TabContent), new UIPropertyMetadata(false, OnIsCachedChanged));

        public static DataTemplate GetTemplate(DependencyObject depObj)
        {
            return (DataTemplate)depObj.GetValue(TemplateProperty);
        }

        public static void SetTemplate(DependencyObject depObj, DataTemplate value)
        {
            depObj.SetValue(TemplateProperty, value);
        }

        /// <summary>
        /// Used instead of TabControl.ContentTemplate for cached tabs
        /// </summary>
        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.RegisterAttached("Template", typeof(DataTemplate), typeof(TabContent), new UIPropertyMetadata(null));

        public static DataTemplateSelector GetTemplateSelector(DependencyObject depObj)
        {
            return (DataTemplateSelector)depObj.GetValue(TemplateSelectorProperty);
        }

        public static void SetTemplateSelector(DependencyObject depObj, DataTemplateSelector value)
        {
            depObj.SetValue(TemplateSelectorProperty, value);
        }

        /// <summary>
        /// Used instead of TabControl.ContentTemplateSelector for cached tabs
        /// </summary>
        public static readonly DependencyProperty TemplateSelectorProperty =
            DependencyProperty.RegisterAttached("TemplateSelector", typeof(DataTemplateSelector), typeof(TabContent), new UIPropertyMetadata(null));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TabControl GetInternalTabControl(DependencyObject depObj)
        {
            return (TabControl)depObj.GetValue(InternalTabControlProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalTabControl(DependencyObject depObj, TabControl value)
        {
            depObj.SetValue(InternalTabControlProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalTabControl.  This enables animation, styling, binding, etc...
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty InternalTabControlProperty =
            DependencyProperty.RegisterAttached("InternalTabControl", typeof(TabControl), typeof(TabContent), new UIPropertyMetadata(null, OnInternalTabControlChanged));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ContentControl? GetInternalCachedContent(DependencyObject depObj)
        {
            return (ContentControl)depObj.GetValue(InternalCachedContentProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalCachedContent(DependencyObject depObj, ContentControl value)
        {
            depObj.SetValue(InternalCachedContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalCachedContent.  This enables animation, styling, binding, etc...
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty InternalCachedContentProperty =
            DependencyProperty.RegisterAttached("InternalCachedContent", typeof(ContentControl), typeof(TabContent), new UIPropertyMetadata(null));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object? GetInternalContentManager(DependencyObject depObj)
        {
            return depObj.GetValue(InternalContentManagerProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalContentManager(DependencyObject depObj, object value)
        {
            depObj.SetValue(InternalContentManagerProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalContentManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InternalContentManagerProperty =
            DependencyProperty.RegisterAttached("InternalContentManager", typeof(object), typeof(TabContent), new UIPropertyMetadata(null));

        private static void OnIsCachedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj == null) return;

            var tabControl = obj as TabControl;
            if (tabControl == null)
            {
                throw new InvalidOperationException("Cannot set TabContent.IsCached on object of type " + args.NewValue.GetType().Name +
                    ". Only objects of type TabControl can have TabContent.IsCached property.");
            }

            bool newValue = (bool)args.NewValue;

            if (!newValue)
            {
                if (args.OldValue != null && ((bool)args.OldValue))
                {
                    throw new NotImplementedException("Cannot change TabContent.IsCached from True to False. Turning tab caching off is not implemented");
                }

                return;
            }

            EnsureContentTemplateIsNull(tabControl);
            tabControl.ContentTemplate = CreateContentTemplate();
        }

        private static DataTemplate CreateContentTemplate()
        {
            const string xaml =
                "<DataTemplate><Border b:TabContent.InternalTabControl=\"{Binding RelativeSource={RelativeSource AncestorType=TabControl}}\" /></DataTemplate>";

            var context = new ParserContext
            {
                XamlTypeMapper = new XamlTypeMapper(Array.Empty<string>())
            };
            context.XamlTypeMapper.AddMappingProcessingInstruction("b", typeof(TabContent).Namespace, typeof(TabContent).Assembly.FullName);

            context.XmlnsDictionary.Add(string.Empty, "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("b", "b");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }

        private static void OnInternalTabControlChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj == null) return;
            var container = obj as Decorator;

            if (container == null)
            {
                var message = "Cannot set TabContent.InternalTabControl on object of type " + obj.GetType().Name +
                    ". Only controls that derive from Decorator, such as Border can have a TabContent.InternalTabControl.";
                throw new InvalidOperationException(message);
            }

            if (args.NewValue == null) return;
            if (!(args.NewValue is TabControl))
            {
                throw new InvalidOperationException("Value of TabContent.InternalTabControl cannot be of type " + args.NewValue.GetType().Name + ", it must be of type TabControl");
            }

            var tabControl = (TabControl)args.NewValue;
            var contentManager = GetContentManager(tabControl, container);
            contentManager.UpdateSelectedTab();
        }

        private static ContentManager GetContentManager(TabControl tabControl, Decorator container)
        {
            var contentManager = (ContentManager)GetInternalContentManager(tabControl);
            if (contentManager != null)
            {
                /*
                 * Content manager already exists for the tab control. This means that tab content template is applied
                 * again, and new instance of the Border control (container) has been created. The old container
                 * referenced by the content manager is no longer visible and needs to be replaced
                 */
                contentManager.ReplaceContainer(container);
            }
            else
            {
                // create content manager for the first time
                contentManager = new ContentManager(tabControl, container);
                SetInternalContentManager(tabControl, contentManager);
            }

            return contentManager;
        }

        private static void EnsureContentTemplateIsNull(TabControl tabControl)
        {
            if (tabControl.ContentTemplate != null)
            {
                throw new InvalidOperationException("TabControl.ContentTemplate value is not null. If TabContent.IsCached is True, use TabContent.Template instead of ContentTemplate");
            }
        }

        internal class ContentManager
        {
            readonly TabControl tabControl;
            Decorator border;

            public ContentManager(TabControl tabControl, Decorator border)
            {
                this.tabControl = tabControl;
                this.border = border;
                tabControl.SelectionChanged += (sender, args) => { UpdateSelectedTab(); };
            }

            public void ReplaceContainer(Decorator newBorder)
            {
                if (ReferenceEquals(border, newBorder)) return;

                border.Child = null; // detach any tab content that old border may hold
                border = newBorder;
            }

            public void UpdateSelectedTab()
            {
                border.Child = GetCurrentContent();
                // EnhancedFocusScope.SetFocusOnActiveElementInScope(_border.Child);
                border.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => EnhancedFocusScope.SetFocusOnActiveElementInScope(border.Child)));
            }

            private ContentControl? GetCurrentContent()
            {
                var item = tabControl.SelectedItem;
                if (item == null) return null;

                var tabItem = tabControl.ItemContainerGenerator.ContainerFromItem(item);
                if (tabItem == null) return null;

                var cachedContent = TabContent.GetInternalCachedContent(tabItem);
                if (cachedContent == null)
                {
                    cachedContent = new ContentControl
                    {
                        DataContext = item,
                        ContentTemplate = TabContent.GetTemplate(tabControl),
                        ContentTemplateSelector = TabContent.GetTemplateSelector(tabControl),
                        Focusable = false
                    };

                    EnhancedFocusScope.SetIsEnhancedFocusScope(cachedContent, true);

                    cachedContent.SetBinding(ContentControl.ContentProperty, new Binding());
                    TabContent.SetInternalCachedContent(tabItem, cachedContent);
                }

                return cachedContent;
            }
        }
    }
}
