using Hercules.Forms.Elements;
using Hercules.Forms.Schema.Custom;
using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    public class BreadcrubmsSetItemBehavior : Behavior<RadBreadcrumb>
    {
        private BreadcrumbsElement ViewModel => (BreadcrumbsElement)AssociatedObject.DataContext;
        private bool isLoaded;

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.CurrentItemChanged += AssociatedObject_CurrentItemChanged;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.CurrentItemChanged -= AssociatedObject_CurrentItemChanged;
            base.OnDetaching();
        }

        private void AssociatedObject_Loaded(object? sender, RoutedEventArgs e)
        {
            isLoaded = true;
            AssociatedObject.CurrentItem = GetElement(ViewModel.Json.AsString);
        }

        private void AssociatedObject_CurrentItemChanged(object? sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (!isLoaded)
                return;

            var viewmodelElement = GetElement(ViewModel.Json.AsString);
            if (AssociatedObject.CurrentItem == viewmodelElement)
                return;

            ((StringElement)ViewModel.Element).Value = AssociatedObject.Path;
        }

        private BreadcrumbsData? GetElement(string path)
        {
            BreadcrumbsData? currentItem = null;
            if (string.IsNullOrEmpty(path) || ViewModel.Items?.Count == 0)
                return currentItem;

            string[] names = path.Split('.');
            IReadOnlyList<BreadcrumbsData> rootItems = ViewModel.Items;

            try
            {
                do
                {
                    var currentItemCandidate = rootItems.SingleOrDefault(x => x.Name == names[0]);
                    if (currentItemCandidate == null)
                        break;

                    currentItem = currentItemCandidate;
                    rootItems = currentItem.Items;
                    names = names.Skip(1).ToArray();
                } while (names.Length > 0 && rootItems?.Count > 0);
            }
            catch (System.Exception)
            { }

            return currentItem;
        }
    }
}