using Hercules.Controls;
using Hercules.Converters;
using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Hercules.Forms.Presentation
{
    public class ElementPresentationProxy
    {
        private VirtualRow? row0;
        private VirtualRow? row1;
        public VirtualRowItem? Item0 { get; set; }
        public VirtualRowItem? Item1 { get; set; }
        public VirtualRowItem? Item2 { get; set; }

        public ElementPresentationProxy(Element element)
        {
            Element = element;
        }

        public Element Element { get; }

        public VirtualRow GetRow(FormPresentation presentation, int index)
        {
            if (index == 0) return row0 ??= new VirtualRow(presentation, Element);
            if (index == 1) return row1 ??= new VirtualRow(presentation, Element);
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void Reset()
        {
            row0 = null;
            row1 = null;
            Item0 = null;
            Item1 = null;
            Item2 = null;
        }
    }

    public class PresentationContext
    {
        public const double VerticalRowMargin = 3;
        public double Left { get; private set; }
        public VirtualRow? First { get; private set; }
        public bool Compact { get; set; }
        public int? GridColumn { get; set; }

        private VirtualRow? current;
        private FormPresentation Presentation { get; }
        private readonly double maxWidth;
        private readonly int version;
        private readonly Stack<double> indents = new();
        private double margin;
        private readonly List<VirtualRowItem> currentItems = new();

        public bool IsPropertyEditor => Presentation is ElementPresentation;

        public PresentationContext(FormPresentation presentation, double maxWidth, int version)
        {
            this.Presentation = presentation;
            this.maxWidth = maxWidth;
            this.version = version;
        }

        public HorizontalDock FlexibleDock => IsPropertyEditor ? HorizontalDock.Fill : Compact ? HorizontalDock.Flexible : HorizontalDock.Left;
        public HorizontalDock FillDock => HorizontalDock.Fill;
        public HorizontalDock RightDock => Compact && !IsPropertyEditor ? HorizontalDock.Left : HorizontalDock.Right;

        public ElementPresentationProxy GetProxy(Element element)
        {
            if (!Presentation.CachedProxies.TryGetValue(element, out var proxy))
            {
                proxy = new ElementPresentationProxy(element);
                Presentation.CachedProxies.Add(element, proxy);
            }
            return proxy;
        }

        public void AddRow(ElementPresentationProxy proxy, int index = 0)
        {
            var newRow = proxy.GetRow(Presentation, index);
            Compact = false;
            Left = indents.Count == 0 ? 0 : indents.Peek();
            newRow.Next = null;
            newRow.Prev = current;
            if (current == null)
            {
                First = newRow;
                newRow.Top = 0;
            }
            else
            {
                current.SetItems(currentItems);
                current.Next = newRow;
                newRow.Top = current.Top + current.Height;
            }
            currentItems.Clear();
            current = newRow;
            current.Height = 23;
            current.Version = version;
            current.LeftMargin = Left;
        }

        public void AddMargin(double width)
        {
            Left += width;
            margin += width;
        }

        public void AddItem(VirtualRowItem item, double height = 0)
        {
            if (current == null)
                throw new InvalidOperationException("AddChild called without AddRow");

            double width = item.Width ?? double.MaxValue;
            var availableWidth = maxWidth - Left;
            if (width > availableWidth)
                width = availableWidth;
            if (width < 0)
                width = 10;
            Left += width;

            if (height + VerticalRowMargin > current.Height)
                current.Height = height + VerticalRowMargin;

            item.Row = current;
            if (GridColumn.HasValue)
                item.GridColumn = GridColumn;
            currentItems.Add(item);
            item.LeftMargin = margin;
            margin = 0;
        }

        public void Seal()
        {
            if (current != null)
            {
                current.SetItems(currentItems);
                currentItems.Clear();
            }
        }

        public void Indent(double offset)
        {
            indents.Push(Math.Max(offset, 0));
        }

        public void Outdent()
        {
            if (current == null)
                throw new InvalidOperationException("Outdent called without AddRow");
            indents.Pop();
            if (currentItems.Count == 0)
                Left = indents.Count == 0 ? 0 : indents.Peek();
        }
    }

    public class VirtualRowItem
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.RegisterAttached("Item", typeof(VirtualRowItem), typeof(VirtualRowItem), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static VirtualRowItem? GetItem(DependencyObject d) => (VirtualRowItem)d.GetValue(ItemProperty);
        public static void SetItem(DependencyObject d, VirtualRowItem? value) => d.SetValue(ItemProperty, value);

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(VirtualRowItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        public static bool GetIsSelected(DependencyObject d) => (bool)d.GetValue(IsSelectedProperty);
        public static void SetIsSelected(DependencyObject d, bool value) => d.SetValue(IsSelectedProperty, value);

        public VirtualRow Row { get; set; } = null!;

        public Element Element { get; }
        public double LeftMargin { get; set; }
        public IControlPool Pool { get; }
        public IControlPool? EditorPool { get; }
        public IControlPool? PopupPool { get; }
        public bool IsTabStop { get; }
        public bool IsSelectable { get; }
        public HorizontalDock Dock { get; }
        public int? GridColumn { get; set; }
        public double? Width { get; }

        private FrameworkElement? preview;
        private Panel? container;
        public FrameworkElement? Editor { get; private set; }
        public Popup? Popup { get; private set; }
        public FrameworkElement? View => container;
        public FrameworkElement? FocusView => Editor ?? View;

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    if (View != null)
                        SetIsSelected(View, isSelected);
                }
            }
        }

        private List<VirtualRowItemBehavior>? behaviors;
        public IReadOnlyList<VirtualRowItemBehavior> Behaviors => behaviors ??= new List<VirtualRowItemBehavior>();

        public VirtualRowItem(Element content, IControlPool pool, IControlPool? editorPool = null, IControlPool? popupPool = null, HorizontalDock dock = HorizontalDock.Left, bool isTabStop = false, bool isSelectable = false, int? gridColumn = null, double? width = null)
        {
            Element = content;
            Pool = pool;
            EditorPool = editorPool;
            PopupPool = popupPool;
            Dock = dock;
            IsTabStop = isTabStop || editorPool != null;
            IsSelectable = isSelectable || IsTabStop;
            GridColumn = gridColumn;
            Width = width;
        }

        public void AddBehavior(VirtualRowItemBehavior behavior)
        {
            behaviors ??= new List<VirtualRowItemBehavior>();
            behaviors.Add(behavior);
        }

        public FrameworkElement CreateVisual()
        {
            Debug.Assert(container == null);
            container = (Panel)VirtualRow.GridPool.Acquire();
            Debug.Assert(container.Children.Count == 0);
            preview = Pool.Acquire();
            Pool.Bind(preview, Element);
            HorizontalDockPanel.SetDock(container, Dock);
            if (Dock == HorizontalDock.Flexible || Dock == HorizontalDock.Fill)
            {
                container.SetValue(FrameworkElement.MinWidthProperty, 80.0);
                if (Dock == HorizontalDock.Flexible)
                {
                    container.SetValue(FrameworkElement.MaxWidthProperty, Width ?? 320);
                }
                container.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            }
            else if (Width.HasValue)
            {
                container.SetValue(FrameworkElement.WidthProperty, Width.Value);
            }

            Thickness thickness = new Thickness(LeftMargin, 0, 0, 0);
            container.SetValue(FrameworkElement.MarginProperty, thickness);
            if (GridColumn.HasValue)
                Grid.SetColumn(container, GridColumn.Value);
            SetItem(container, this);
            container.Children.Add(preview);
            if (behaviors != null)
                foreach (var behavior in behaviors)
                {
                    behavior.OnCreateVisual(container);
                }
            return container;
        }

        public FrameworkElement CreateEditor()
        {
            if (Editor == null)
            {
                Editor = EditorPool!.Acquire();
                EditorPool.Bind(Editor, Element);
                container!.Children.Add(Editor);
            }
            return Editor;
        }

        public Popup CreatePopup(bool fixedWidth)
        {
            if (Popup == null)
            {
                Popup = new Popup { PlacementTarget = container, Placement = PlacementMode.Bottom };
                var popupContent = PopupPool!.Acquire();
                if (fixedWidth)
                    Popup.Width = container!.ActualWidth;
                else
                    Popup.MinWidth = container!.ActualWidth;
                PopupPool.Bind(popupContent, Element);
                Popup.Child = popupContent;
                container!.Children.Add(Popup);
            }
            return Popup;
        }

        public void DisposePopup()
        {
            if (Popup != null)
            {
                container!.Children.Remove(Popup);
                var popupChild = (FrameworkElement)Popup.Child;
                Popup.Child = null;
                PopupPool!.Bind(popupChild, null);
                PopupPool!.Release(popupChild);
                Popup = null;
            }
        }

        public void DisposeEditor()
        {
            if (Editor != null)
            {
                container!.Children.Remove(Editor);
                EditorPool!.Bind(Editor, null);
                EditorPool!.Release(Editor);
                Editor = null;
            }
        }

        public void DisposeVisual()
        {
            if (behaviors != null)
                foreach (var behavior in behaviors)
                {
                    behavior.OnDisposeVisual(View!);
                }

            DisposePopup();
            DisposeEditor();
            Debug.Assert(container != null);
            Debug.Assert(preview != null);
            container.Children.Remove(preview);
            container.ClearValue(ItemProperty);
            container.ClearValue(IsSelectedProperty);
            container.ClearValue(Grid.ColumnProperty);
            container.ClearValue(FrameworkElement.WidthProperty);
            container.ClearValue(FrameworkElement.MinWidthProperty);
            container.ClearValue(FrameworkElement.MaxWidthProperty);
            container.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
            Debug.Assert(container.Children.Count == 0);
            VirtualRow.GridPool.Release(container);
            container = null;
            Pool.Bind(preview, null);
            Pool.Release(preview);
            preview = null;
        }

        public void Select()
        {
            if (View == null || IsSelected)
                return;
            IsSelected = true;
            if (EditorPool == null)
                return;
            CreateEditor();
            if (behaviors != null)
                foreach (var behavior in behaviors)
                {
                    behavior.OnSelect();
                }
        }

        public void Deselect()
        {
            if (View == null || !IsSelected)
                return;
            IsSelected = false;
            if (behaviors != null)
                foreach (var behavior in behaviors)
                {
                    behavior.OnDeselect();
                }
            DisposeEditor();
            DisposePopup();
        }
    }

    public class VirtualRow : IVirtualRow
    {
        public FormPresentation Presentation { get; }

        public VirtualRow(FormPresentation presentation, Element ownerElement)
        {
            this.Presentation = presentation;
            OwnerElement = ownerElement;
        }

        public static readonly IControlPool GridPool = new FactoryControlPool(() => new Grid(), "Grid");

        public List<VirtualRowItem> Items { get; } = new();
        private Panel? rootPanel;
        private Panel? defaultPanel;

        public double LeftMargin { get; set; }
        public double Top { get; set; }

        public double Height { get; set; }

        public int Version { get; set; }

        public bool Pinned { get; set; }

        public UIElement? Visual => rootPanel;

        public UIElement CreateVisual(VirtualCanvas parent)
        {
            if (rootPanel == null)
            {
                Presentation.CreateRowPanel(out rootPanel, out defaultPanel);
                //uiRoot.Width = parent.ScrollOwner.ViewportWidth - left;
                rootPanel.SetValue(Canvas.LeftProperty, LeftMargin);
                Binding actualWidthBinding = new Binding
                {
                    Source = parent,
                    Path = new PropertyPath(nameof(FrameworkElement.ActualWidth)),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = ActualWidthConverter.Default,
                    ConverterParameter = LeftMargin
                };

                Binding inheritedBinding = new Binding
                {
                    Source = OwnerElement,
                    Path = new PropertyPath(nameof(Element.IsInherited)),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = InheritedOpacityConverter.Default
                };

                BindingOperations.SetBinding(rootPanel, FrameworkElement.WidthProperty, actualWidthBinding);
                BindingOperations.SetBinding(rootPanel, UIElement.OpacityProperty, inheritedBinding);
                rootPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            }

            rootPanel.Height = Height;
            foreach (var item in Items)
            {
                if (item.View == null)
                {
                    CreateVisualItem(item);
                }
            }
            return rootPanel;
        }

        private void CreateVisualItem(VirtualRowItem item)
        {
            var panel = item.GridColumn.HasValue ? rootPanel! : defaultPanel!;
            panel.Children.Add(item.CreateVisual());
        }

        public void DisposeVisual()
        {
            if (rootPanel != null)
            {
                foreach (var item in Items)
                {
                    if (item.View != null)
                    {
                        DisposeVisualItem(item);
                    }
                }
                BindingOperations.ClearBinding(rootPanel, FrameworkElement.WidthProperty);
                BindingOperations.ClearBinding(rootPanel, UIElement.OpacityProperty);
                Presentation.ReleaseRowPanel(rootPanel, defaultPanel!);
                rootPanel = null;
                defaultPanel = null;
            }
        }

        private void DisposeVisualItem(VirtualRowItem item)
        {
            var panel = item.GridColumn.HasValue ? rootPanel! : defaultPanel!;
            Debug.Assert(panel != null && panel.Children.Contains(item.View));
            panel.Children.Remove(item.View);
            item.DisposeVisual();
        }

        IVirtualRow? IVirtualRow.Next => Next;
        public VirtualRow? Next { get; set; }
        public VirtualRow? Prev { get; set; }
        public Element OwnerElement { get; }

        public override string? ToString()
        {
            return OwnerElement.ToString();
        }

        public bool GetNextTabStop(VirtualRowItem? currentItem, [MaybeNullWhen(false)] out VirtualRowItem nextItem)
        {
            nextItem = currentItem == null ?
                Items.FirstOrDefault(item => item.IsTabStop && item.Element.IsActive) :
                Items.SkipWhile(item => item != currentItem).SkipWhile(item => item == currentItem).FirstOrDefault(item => item.IsTabStop && item.Element.IsActive);

            if (nextItem != null)
            {
                return true;
            }
            else if (Next != null)
            {
                return Next.GetNextTabStop(null, out nextItem);
            }
            else
            {
                nextItem = null;
                return false;
            }
        }

        public bool GetPrevTabStop(VirtualRowItem? currentItem, [MaybeNullWhen(false)] out VirtualRowItem prevItem)
        {
            prevItem = currentItem == null ?
                Items.LastOrDefault(item => item.IsTabStop && item.Element.IsActive) :
                Enumerable.Reverse(Items).SkipWhile(item => item != currentItem).SkipWhile(item => item == currentItem).FirstOrDefault(item => item.IsTabStop && item.Element.IsActive);

            if (prevItem != null)
            {
                return true;
            }
            else if (Prev != null)
            {
                return Prev.GetPrevTabStop(null, out prevItem);
            }
            else
            {
                prevItem = null;
                return false;
            }
        }

        public void SetItems(IReadOnlyList<VirtualRowItem> newItems)
        {
            bool shouldUpdateVisual = Visual != null && (Items.Count != newItems.Count || newItems.Any(item => item.View == null));
            if (shouldUpdateVisual)
            {
                var removeItems = Items.Except(newItems).ToList();
                var addItems = newItems.Except(Items).ToList();
                foreach (var item in removeItems)
                {
                    DisposeVisualItem(item);
                }
                foreach (var item in addItems)
                {
                    CreateVisualItem(item);
                }
            }
            Items.Clear();
            Items.AddRange(newItems);
        }
    }

    public abstract class VirtualRowItemBehavior
    {
        public VirtualRowItem Item { get; }

        protected VirtualRowItemBehavior(VirtualRowItem item)
        {
            Item = item;
        }

        public virtual void OnSelect() { }
        public virtual void OnDeselect() { }

        public virtual void OnCreateVisual(FrameworkElement view) { }
        public virtual void OnDisposeVisual(FrameworkElement view) { }
    }
}
