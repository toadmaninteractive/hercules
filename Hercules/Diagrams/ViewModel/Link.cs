using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls.Diagrams.Extensions.ViewModels;

namespace Hercules.Diagrams
{
    public class Link : LinkViewModelBase<BlockBase>
    {
        public bool IsAssetLink { get; private set; } //TODO instead of this needs abstraction

        /// <summary>
        /// Element on Form from "Links"
        /// </summary>
        public LinkListItem? FormListItem { get; private set; }

        string sourceSlot = "Auto";

        public string SourceSlot
        {
            get => sourceSlot;
            set
            {
                if (sourceSlot != value)
                {
                    sourceSlot = value;
                    SourceConnector = CheckValid(value, this.Source, new Point(0.5, 1.0));
                    OnPropertyChanged(() => SourceSlot);
                }
            }
        }

        public BaseConnector? SourceConnector { get; private set; }

        string targetSlot = "Auto";

        public string TargetSlot
        {
            get => targetSlot;
            set
            {
                if (targetSlot != value)
                {
                    targetSlot = value;
                    TargetConnector = CheckValid(value, this.Target, new Point(0.5, 0.0));
                    OnPropertyChanged(() => TargetSlot);
                }
            }
        }

        public BaseConnector? TargetConnector { get; private set; }

        public Link()
        {
        }

        public Link(BlockBase source, BlockBase target, string sourceSlot, string targetSlot, LinkListItem? linkItem = null)
            : base(source, target)
        {
            Source = source;
            Target = target;
            SourceSlot = sourceSlot;
            TargetSlot = targetSlot;
            IsAssetLink = linkItem == null;
            FormListItem = linkItem;
        }

        private BaseConnector? CheckValid(string value, BlockBase? block, Point invalidConnectorOffset)
        {
            if (block == null)
                return null;

            var connector = block.Connectors.FirstOrDefault(x => x.Name == value) as BaseConnector;
            if (connector == null)
            {
                connector = new SimpleConnector(block, SchemaConnector.InvalidConnector, Visibility.Hidden)
                {
                    Offset = invalidConnectorOffset,
                    Name = value,
                };
                block.Connectors.Add(connector);
            }
            return connector;
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}