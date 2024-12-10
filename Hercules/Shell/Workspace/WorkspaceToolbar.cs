using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hercules.Shell
{
    public class WorkspaceToolbar
    {
        public ObservableCollection<UiOption> Buttons { get; } = new();

        public void Build(IEnumerable<UiMenuOption> options)
        {
            Buttons.Clear();
            UiCategory? lastCategory = null;
            foreach (var option in options)
            {
                if (lastCategory.HasValue && option.CategoryPath.Parts[0] != lastCategory)
                    Buttons.Add(UiSeparator.Instance);
                lastCategory = option.CategoryPath.Parts[0];
                Buttons.Add(option.Option);
            }
        }
    }
}
