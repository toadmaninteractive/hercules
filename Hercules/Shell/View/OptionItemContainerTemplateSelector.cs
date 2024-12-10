using System.Windows;
using System.Windows.Controls;

namespace Hercules.Shell.View
{
    public class OptionItemContainerTemplateSelector : ItemContainerTemplateSelector
    {
        public DataTemplate? ToggleTemplate { get; set; }
        public DataTemplate? CommandTemplate { get; set; }
        public DataTemplate? SeparatorTemplate { get; set; }
        public DataTemplate? CategoryTemplate { get; set; }
        public DataTemplate? AdviceTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            var template = item switch
            {
                UiSeparator => SeparatorTemplate,
                UiToggleOption => ToggleTemplate,
                UiCommandOption => CommandTemplate,
                UiCustomOption uiCustomOption => (DataTemplate)Application.Current.FindResource(uiCustomOption.ItemContainerTemplateKey),
                UiCategoryOption => CategoryTemplate,
                AdviceOption => AdviceTemplate,
                _ => null
            };
            return template ?? base.SelectTemplate(item, parentItemsControl);
        }
    }
}
