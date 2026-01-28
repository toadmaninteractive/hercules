using System.Windows;
using System.Windows.Controls;

namespace Hercules.Diagrams.View
{
    public class LinkStyleSelector : StyleSelector
    {
        public Style LinkStyle { get; set; } = default!;

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return LinkStyle;
        }
    }
}