using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Windows;

namespace Hercules.Diagrams
{
    public class BlockStart : BlockBase
    {
        public BlockStart(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
            this.VisibilityInOutConnectors = Visibility.Hidden;
        }
    }
}