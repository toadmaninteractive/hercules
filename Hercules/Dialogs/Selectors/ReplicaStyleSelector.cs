using System.Windows;
using System.Windows.Controls;

namespace Hercules.Dialogs
{
    public class ReplicaStyleSelector : StyleSelector
    {
        public Style? ReplicaStyle { get; set; }
        public Style? VirtualReplicaStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return item switch
            {
                VirtualReplicaViewModel => VirtualReplicaStyle,
                _ => ReplicaStyle
            } ?? base.SelectStyle(item, container);
        }
    }
}
