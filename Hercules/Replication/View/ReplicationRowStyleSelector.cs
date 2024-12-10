using Hercules.DB;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Replication.View
{
    public class ReplicationRowStyleSelector : StyleSelector
    {
        public Style? AddedStyle { get; set; }
        public Style? DeletedStyle { get; set; }
        public Style? ModifiedStyle { get; set; }
        public Style? NormalStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var row = item as DatabaseComparerEntry;
            if (row == null)
                return base.SelectStyle(item, container);

            Style? style = NormalStyle;
            if (row.Selected != false)
            {
                style = row.ChangeType switch
                {
                    DocumentCommitType.Added => AddedStyle,
                    DocumentCommitType.Deleted => DeletedStyle,
                    DocumentCommitType.Modified => ModifiedStyle,
                    _ => NormalStyle
                };
            }
            return style ?? base.SelectStyle(item, container);
        }
    }
}
