using System.Collections.ObjectModel;

namespace Hercules.Shell
{
    // A tab strip of the document area; the window has a main group and a side group to the right of it
    public sealed class EditorGroup : NotifyPropertyChanged
    {
        public ObservableCollection<Page> Pages { get; } = new();

        public Page? SelectedPage
        {
            get;
            set => SetField(ref field, value);
        }
    }
}
