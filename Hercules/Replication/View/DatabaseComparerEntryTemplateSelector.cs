using System.Windows;
using System.Windows.Controls;

namespace Hercules.Replication.View
{
    public class DatabaseComparerEntryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DocumentTemplate { get; set; }
        public DataTemplate? DiffTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var template = item switch
            {
                DatabaseComparerDocumentEntry => DocumentTemplate,
                DatabaseComparerDiffEntry => DiffTemplate,
                _ => null
            };
            return template ?? base.SelectTemplate(item, container);
        }
    }
}
