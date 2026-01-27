using Hercules.Controls;
using Hercules.Forms.Elements;
using NPOI.SS.Formula.Functions;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            Form.OnExpandIntoView += ExpandIntoView;
        }

        public Element? SelectedElement => SelectedItem?.Element;
        public VirtualRowItem? SelectedItem { get; private set; }

        public event Action<VirtualRowItem>? OnFocusItem;
        public event Action<VirtualRow, VirtualRow?>? OnScrollIntoView;
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
                    ScrollIntoView(item.Row, null);
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

        private void ExpandIntoView(Element element)
        {
            if (GetRowRange(element, out var fromRow, out var toRow))
                ScrollIntoView(fromRow, toRow);
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

        private void ScrollIntoView(VirtualRow row, VirtualRow? toRow)
        {
            OnScrollIntoView?.Invoke(row, toRow);
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

        private bool GetRowRange(Element element, [MaybeNullWhen(returnValue: false)] out VirtualRow fromRow, [MaybeNullWhen(returnValue: false)] out VirtualRow toRow)
        {
            var row = FirstRow;
            bool found = false;
            fromRow = null!;
            toRow = null!;
            while (row != null)
            {
                bool isInRange = element == row.OwnerElement || row.OwnerElement.IsChildOf(element) || row.Items.Any(item => item.Element == element || item.Element.IsChildOf(element));
                if (isInRange)
                {
                    if (!found)
                    {
                        fromRow = row;
                        found = true;
                    }
                    toRow = row;
                }
                else if (found)
                    return true;
                row = row.Next;
            }

            return false;
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
