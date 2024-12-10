using Hercules.Controls;
using Hercules.Forms.Elements;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Hercules.Forms.Presentation
{
    public class FormPresentation
    {
        protected static readonly IControlPool HorizontalDockPanelPool = new FactoryControlPool(() => new HorizontalDockPanel(), "HorizontalDockPanel");
        public ConditionalWeakTable<Element, ElementPresentationProxy> CachedProxies { get; } = new();

        public DocumentForm Form { get; }

        public FormPresentation(DocumentForm form)
        {
            Form = form;
            Form.OnRefreshPresentation += RefreshPresentation;
        }

        public Element? SelectedElement => SelectedItem?.Element;
        public VirtualRowItem? SelectedItem { get; private set; }

        public event Action<VirtualRowItem>? OnFocusItem;
        public event Action<VirtualRow>? OnScrollIntoView;
        public event Action? OnRefreshItems;
        public int ViewVersion { get; private set; }

        public VirtualRow? FirstRow { get; private set; }

        public void SelectElement(Element element)
        {
            Select(GetFirstElementItem(element, selectable: true));
        }

        public void Select(VirtualRowItem? item)
        {
            if (SelectedItem != item)
            {
                Form.SealUndo();
                if (SelectedItem != null)
                {
                    SelectedItem.Row.Pinned = false;
                    SelectedItem.Deselect();
                }

                SelectedItem = item;
                if (item != null)
                {
                    ScrollIntoView(item.Row);
                    item.Row.Pinned = true;
                    item.Select();
                    OnFocusItem?.Invoke(item);
                }
            }
        }

        public void RefreshPresentation()
        {
            ViewVersion++;
            var context = new PresentationContext(this, 1000, ViewVersion);
            Present(context);
            context.Seal();
            FirstRow = context.First;
            OnRefreshItems?.Invoke();
            if (SelectedItem != null && SelectedItem.Row.Version != ViewVersion)
            {
                ClearSelection();
            }
        }

        protected virtual void Present(PresentationContext context)
        {
            Form.Root.Present(context);
        }

        public virtual void CreateRowPanel(out Panel rootPanel, out Panel defaultPanel)
        {
            rootPanel = (Panel)HorizontalDockPanelPool.Acquire();
            Debug.Assert(rootPanel.Children.Count == 0);
            defaultPanel = rootPanel;
        }

        public virtual void ReleaseRowPanel(Panel rootPanel, Panel defaultPanel)
        {
            Debug.Assert(rootPanel == defaultPanel);
            Debug.Assert(rootPanel is HorizontalDockPanel);
            Debug.Assert(rootPanel.Children.Count == 0);
            HorizontalDockPanelPool.Release(rootPanel);
        }

        private void ScrollIntoView(VirtualRow row)
        {
            OnScrollIntoView?.Invoke(row);
        }

        public void ClearSelection()
        {
            Select(null);
        }

        private VirtualRowItem? GetFirstElementItem(Element element, bool selectable = false, bool tabStop = false)
        {
            var row = FirstRow;
            while (row != null)
            {
                foreach (var item in row.Items)
                {
                    if (item.Element == element && (item.IsSelectable || !selectable) && (item.IsTabStop || !tabStop))
                        return item;
                }
                row = row.Next;
            }

            return null;
        }

        private bool GetPrevTabItem(VirtualRowItem? currentItem, [MaybeNullWhen(false)] out VirtualRowItem prevItem)
        {
            if (FirstRow != null && currentItem != null && currentItem.Row.GetPrevTabStop(currentItem, out prevItem))
            {
                return true;
            }
            else
            {
                prevItem = default;
                return false;
            }
        }

        private bool GetNextTabItem(VirtualRowItem? currentItem, [MaybeNullWhen(false)] out VirtualRowItem nextItem)
        {
            if (FirstRow == null)
            {
                nextItem = default!;
                return false;
            }

            if (currentItem != null && currentItem.Row.GetNextTabStop(currentItem, out nextItem))
                return true;
            else
                return FirstRow.GetNextTabStop(null, out nextItem);
        }

        public void NextTab()
        {
            if (GetNextTabItem(SelectedItem, out var nextItem))
            {
                Select(nextItem);
            }
        }

        public void PrevTab()
        {
            if (GetPrevTabItem(SelectedItem, out var nextItem))
            {
                Select(nextItem);
            }
        }
    }
}
