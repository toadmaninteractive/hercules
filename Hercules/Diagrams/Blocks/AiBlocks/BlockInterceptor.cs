using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Windows;

namespace Hercules.Diagrams
{
    public class BlockInterceptor : BlockBase
    {
        public BlockInterceptor(SchemaBlock prototype, BlockListItem element) : base(prototype, element)
        {
            VisibilityInOutConnectors = Visibility.Hidden;
            VisibilityPropertyConnectors = Visibility.Hidden;
        }
    }
}