using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Hercules
{
    /// <summary>
    /// ObservableCollection that inserts and removes ranges with a single Reset notification.
    /// </summary>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void RemoveRange(int index, int count)
        {
            this.CheckReentrancy();
            var items = (List<T>)Items;
            items.RemoveRange(index, count);
            OnReset();
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            this.CheckReentrancy();
            var items = (List<T>)Items;
            items.InsertRange(index, collection);
            OnReset();
        }

        private void OnReset()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
