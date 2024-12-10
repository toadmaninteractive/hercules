using System.Windows;
using System.Windows.Controls;

namespace Hercules.Shell.View
{
    public class OptionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ToggleTemplate { get; set; }
        public DataTemplate? CommandTemplate { get; set; }
        public DataTemplate? SeparatorTemplate { get; set; }
        public DataTemplate? CategoryTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var template = item switch
            {
                UiSeparator => SeparatorTemplate,
                UiToggleOption => ToggleTemplate,
                UiCommandOption => CommandTemplate,
                UiCustomOption customOption => (DataTemplate)Application.Current.FindResource(customOption.ToolbarCommandTemplateKey),
                UiCategoryOption => CategoryTemplate,
                _ => null,
            };
            return template ?? base.SelectTemplate(item, container);
        }
    }
}
