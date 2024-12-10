using Hercules.Forms.Elements;
using Hercules.Forms.Schema;

namespace Hercules.Diagrams
{
    public class BlockCheck : BlockBase
    {
        public BlockCheck(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
            this.VisibilityInOutConnectors = System.Windows.Visibility.Hidden;
            this.VisibilityPropertyConnectors = System.Windows.Visibility.Hidden;
        }
    }
}