using System;
using System.Collections.Generic;
using System.Globalization;

namespace Hercules.Shell
{
    public class UiMenuOption
    {
        public UiOption Option { get; }
        public UiCategoryPath CategoryPath { get; }
        public bool ShowInToolbar { get; }

        public UiMenuOption(UiOption option, UiCategoryPath categoryPath, bool showInToolbar)
        {
            Option = option;
            CategoryPath = categoryPath;
            ShowInToolbar = showInToolbar;
        }
    }

    public class UiOptionManager : IComparer<UiCategoryPath>
    {
        public IReadOnlyList<UiMenuOption> Options => options;

        readonly List<UiMenuOption> options = new();
        readonly Dictionary<string, int> categoryWeights = new();
        readonly Dictionary<Type, List<UiOption>> contextMenus = new();

        public UiOptionManager()
        {
            SetCategoryWeight("Connection", -400);
            SetCategoryWeight("Document", -300);
            SetCategoryWeight("Edit", -200);
            SetCategoryWeight("View", -100);
            SetCategoryWeight("Data", 100);
            SetCategoryWeight("Tools", 200);
            SetCategoryWeight("Window", 300);
            SetCategoryWeight("Help", 400);
        }

        public WorkspaceContextMenu? GetContextMenu<T>()
        {
            if (contextMenus.TryGetValue(typeof(T), out var opts))
                return new WorkspaceContextMenu(opts);
            else
                return null;
        }

        public UiMenuOption AddMenuOption(UiOption option, string menuCategory, bool showInToolbar = false)
        {
            var result = new UiMenuOption(option, new UiCategoryPath(menuCategory), showInToolbar);
            options.Add(result);
            return result;
        }

        public void RemoveMenuOption(UiMenuOption menuOption)
        {
            options.Remove(menuOption);
        }

        public void AddContextMenuOption<T>(UiOption option)
        {
            AddContextMenuOption(typeof(T), option);
        }

        public void AddContextMenuOption(Type targetType, UiOption option)
        {
            if (!contextMenus.TryGetValue(targetType, out var contextMenu))
            {
                contextMenu = new List<UiOption>();
                contextMenus.Add(targetType, contextMenu);
            }
            contextMenu.Add(option);
        }

        public void SetCategoryWeight(string category, int weight)
        {
            categoryWeights[category] = weight;
        }

        public int Compare(UiCategoryPath? x, UiCategoryPath? y)
        {
            if (x is null && y is null)
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;
            int i = 0;
            while (true)
            {
                if (x.Parts.Count == i && y.Parts.Count == i)
                    return 0;
                if (x.Parts.Count == i)
                    return 1;
                if (y.Parts.Count == i)
                    return -1;
                var left = x.Parts[i];
                var right = y.Parts[i];
                int compare = 0;
                if (i == 0)
                {
                    categoryWeights.TryGetValue(left.Name, out var leftWeight);
                    categoryWeights.TryGetValue(right.Name, out var rightWeight);
                    compare = leftWeight - rightWeight;
                }
                if (compare == 0)
                    compare = left.Index - right.Index;
                if (compare == 0)
                    compare = string.Compare(left.Name, right.Name);
                if (compare != 0)
                    return compare;
                i++;
            }
        }
    }
}
