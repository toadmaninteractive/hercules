using Hercules.Forms.Elements;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Diagrams.View
{
    public class PropertyTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? EmptyCellTemplate { get; set; }
        public DataTemplate? TextBlockCellTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var template = item switch
            {
                Field => TextBlockCellTemplate,
                OptionalElement optionalElement => GetTemplateForElement(optionalElement.Element),
                _ => null
            };
            return template ?? base.SelectTemplate(item, container);
        }

        private DataTemplate? GetTemplateForElement(Element element)
        {
            if (element is LocalizedElement)
                return EmptyCellTemplate;

            return TextBlockCellTemplate;
        }
    }
}