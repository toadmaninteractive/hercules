using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Hercules
{
    public static class VisualTreeHelperEx
    {
        public static IEnumerable<DependencyObject> GetParentTree(DependencyObject element)
        {
            if (!(element is Visual))
                yield break;
            while (true)
            {
                element = VisualTreeHelper.GetParent(element);
                if (element != null)
                    yield return element;
                else
                    yield break;
            }
        }

        public static T? GetDescendant<T>(DependencyObject element) where T : DependencyObject
        {
            return GetDescendantByType(element, typeof(T)) as T;
        }

        public static DependencyObject? GetDescendantByType(DependencyObject element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            DependencyObject? foundElement = null;
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                DependencyObject visual = VisualTreeHelper.GetChild(element, i);
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        public static IEnumerable<T> GetDescendants<T>(DependencyObject element) where T : class
        {
            if (element == null)
            {
                yield break;
            }
            if (element is T result)
            {
                yield return result;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                DependencyObject visual = VisualTreeHelper.GetChild(element, i);
                foreach (var foundElement in GetDescendants<T>(visual))
                    yield return foundElement;
            }
        }

        public static IInputElement? GetInputElement(DependencyObject? element)
        {
            if (element == null)
            {
                return null;
            }
            if (element is IInputElement && ((IInputElement)element).Focusable)
            {
                return (IInputElement)element;
            }
            IInputElement? foundElement = null;
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                DependencyObject visual = VisualTreeHelper.GetChild(element, i);
                foundElement = GetInputElement(visual);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        public static T? GetParent<T>(DependencyObject element) where T : class
        {
            if (element == null)
                return null;
            if (element is T result)
                return result;
            return GetParent<T>(VisualTreeHelper.GetParent(element));
        }
    }
}
