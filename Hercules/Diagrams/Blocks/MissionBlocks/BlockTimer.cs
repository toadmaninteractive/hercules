using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Windows;

namespace Hercules.Diagrams
{
    public class BlockTimer : BlockBase
    {
        public BlockTimer(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
            this.VisibilityInOutConnectors = Visibility.Hidden;
        }
    }
}