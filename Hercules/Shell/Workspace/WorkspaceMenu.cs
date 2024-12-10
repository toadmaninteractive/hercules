using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Shell
{
    public class WorkspaceMenu
    {
        public ObservableCollection<UiOption> Items { get; } = new();

        public void Build(IEnumerable<UiMenuOption> options, AdviceManager adviceManager)
        {
            Items.Clear();
            foreach (var group in options.GroupBy(c => c.CategoryPath.Parts[0].Name))
                Items.Add(new UiCategoryOption(group.Key, GetCategory(group, 0)));
            Items.Add(new AdviceOption(adviceManager));
        }

        private static string? GetCategoryName(UiCategoryPath categoryPath, int level)
        {
            if (level >= categoryPath.Parts.Count)
                return null;
            else
                return categoryPath.Parts[level].Name;
        }

        private static IEnumerable<UiOption> GetCategory(IEnumerable<UiMenuOption> commands, int level)
        {
            bool isFirst = true;
            foreach (var group in commands.GroupAdjacentBy((c1, c2) => c1.CategoryPath.Parts[level].Index == c2.CategoryPath.Parts[level].Index))
            {
                if (!isFirst)
                    yield return UiSeparator.Instance;
                isFirst = false;
                foreach (var list in group.GroupAdjacentBy((c1, c2) => GetCategoryName(c1.CategoryPath, level + 1) == GetCategoryName(c2.CategoryPath, level + 1)))
                {
                    var catName = GetCategoryName(list[0].CategoryPath, level + 1);
                    if (catName == null)
                    {
                        foreach (var cmd in list)
                            yield return cmd.Option;
                    }
                    else
                    {
                        yield return new UiCategoryOption(catName, GetCategory(list, level + 1));
                    }
                }
            }
        }
    }

    public interface IWorkspaceContextMenuProvider
    {
        WorkspaceContextMenu? ContextMenu { get; }
    }

    public class WorkspaceContextMenu
    {
        public IReadOnlyList<UiOption> Items { get; }

        public WorkspaceContextMenu(IReadOnlyList<UiOption> items)
        {
            Items = items;
        }
    }
}
