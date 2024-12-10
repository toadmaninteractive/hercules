using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Hercules.Diagrams
{
    public class BlockSelectorViewModel : NotifyPropertyChanged
    {
        public ICommand<ToolBoxItem> SelectCommand { get; }

        public Point Position { get; private set; } = new Point(0, 0);
        public BaseConnector? SourceConnector { get; private set; }

        public ObservableCollection<ToolBoxItem> ToolBoxItems { get; } = new ObservableCollection<ToolBoxItem>();

        public BlockSelectorViewModel(ICommand<ToolBoxItem> selectCommand)
        {
            SelectCommand = selectCommand;
        }

        public void UpdateState(IEnumerable<ToolBoxItem> items, Point position, BaseConnector sourceConnector)
        {
            Position = position;
            SourceConnector = sourceConnector;
            ToolBoxItems.Clear();
            ToolBoxItems.AddRange(items);
        }
    }
}