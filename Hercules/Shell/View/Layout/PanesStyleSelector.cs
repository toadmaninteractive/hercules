using System.Windows;
using System.Windows.Controls;

namespace Hercules.Shell.View
{
    public class PanesStyleSelector : StyleSelector
    {
        public Style? DocumentStyle { get; set; }
        public Style? ToolStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var style = item switch
            {
                Page => DocumentStyle,
                Tool => ToolStyle,
                _ => null,
            };
            return style ?? base.SelectStyle(item, container);
        }
    }
}
