using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls.Diagrams;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Diagrams.View
{
    /// <summary>
    /// Interaction logic for DiagramTab.xaml
    /// </summary>
    [ViewModelType(typeof(DiagramTab))]
    public partial class DiagramTabView : UserControl
    {
        private bool isFirstLoad = true;
        private readonly BlockSelectorViewModel blockSelectorViewModel;
        private DiagramTab Context => (DiagramTab)DataContext;

        public DiagramTabView()
        {
            InitializeComponent();
            blockSelectorViewModel = new BlockSelectorViewModel(Commands.Execute<ToolBoxItem>(BlockSelectorOnSelected));
            PopupBlockSelector.DataContext = blockSelectorViewModel;
            DiagramConstants.MinimumZoom = 0.1;
            DiagramConstants.MaximumZoom = 2;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var selected = Context.Diagram.CurrentSelected;
            if (selected == null)
                return;
            var newPosition = e.Key switch
            {
                Key.Up => (Point?)new Point(selected.Position.X, selected.Position.Y - 1),
                Key.Down => new Point(selected.Position.X, selected.Position.Y + 1),
                Key.Left => new Point(selected.Position.X - 1, selected.Position.Y),
                Key.Right => new Point(selected.Position.X + 1, selected.Position.Y),
                _ => null
            };

            if (newPosition.HasValue)
            {
                selected.Position = newPosition.Value;
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void ConnectionManipulationCompletedHandler(object sender, ManipulationRoutedEventArgs e)
        {
            if (e.Connection.SourceConnectorResult != null)
            {
                var sourceConnector = (BaseConnector)e.Connection.SourceConnectorResult;

                if (e.Connector is BaseConnector targetConnector)
                {
                    if (e.Connection.Source.Id != e.Shape.Id)
                    {
                        Context.Diagram.TryCreateLink(sourceConnector, targetConnector);
                    }
                }
                else
                {
                    ShowPopupBlockSelector(sourceConnector, e.CurrentPosition);
                }
            }

            e.Handled = true;
        }

        private void ShowPopupBlockSelector(BaseConnector sourceConnector, Point currentPosition)
        {
            if (Diagram.IsMouseOver)
            {
                var applicableBlocks = Context.ToolBoxTool.GetApplicableBlocks(sourceConnector);
                if (applicableBlocks.Any())
                {
                    blockSelectorViewModel.UpdateState(applicableBlocks, currentPosition, sourceConnector);
                    PopupBlockSelector.IsOpen = true;
                }
            }
        }

        #region PopupBlockSelector API

        /// <param name="selectedBlock">target BlockBase</param>
        private void BlockSelectorOnSelected(ToolBoxItem selectedBlock)
        {
            Context.Diagram.CreateBlock(selectedBlock.Prototype, blockSelectorViewModel.SourceConnector!, blockSelectorViewModel.Position);
            PopupBlockSelector.IsOpen = false;
        }

        private void PopupBlockSelectorMouseLeave(object sender, MouseEventArgs e)
        {
            PopupBlockSelector.IsOpen = false;
        }

        #endregion PopupBlockSelector API

        private void AutoLayoutHandler(object sender, RoutedEventArgs e)
        {
            Context.Diagram.AutoLayout();
            Diagram.AutoFit();
        }

        private void RadToggleButton(object sender, RoutedEventArgs e)
        {
            switch (Diagram.ActiveTool)
            {
                case MouseTool.ConnectorTool:
                    break;
                case MouseTool.PanTool:
                    Diagram.ActiveTool = MouseTool.PointerTool;
                    BtnPointer.IsChecked = true;
                    BtnPan.IsChecked = false;
                    break;
                case MouseTool.PathTool:
                    break;
                case MouseTool.PencilTool:
                    break;
                case MouseTool.PointerTool:
                    Diagram.ActiveTool = MouseTool.PanTool;
                    BtnPointer.IsChecked = false;
                    BtnPan.IsChecked = true;
                    break;
                case MouseTool.TextTool:
                    break;
            }
        }

        private void DiagramTabLoaded(object sender, RoutedEventArgs e)
        {
            if (isFirstLoad)
            {
                Diagram.AutoFit();
                isFirstLoad = false;
            }
        }

        private void DropHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(HerculesDragData.DragDataFormat))
            {
                if (e.Data.GetData(HerculesDragData.DragDataFormat) is ToolBoxItem dropInfo)
                {
                    var position = DiagramExtensions.GetTransformedPoint(Diagram, e.GetPosition(this.Diagram));
                    Context.Diagram.CreateBlock(dropInfo.Prototype, null, position);
                }
            }
            e.Handled = true;
        }

        private void DragOverHandler(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(HerculesDragData.DragDataFormat))
            {
                if (e.Data.GetData(HerculesDragData.DragDataFormat) is ToolBoxItem dropInfo)
                    e.Effects = DragDropEffects.All;
            }
            e.Handled = true;
        }
    }
}