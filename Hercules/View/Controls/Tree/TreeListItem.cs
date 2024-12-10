using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Controls.Tree
{
    public class TreeListItem : ListViewItem, INotifyPropertyChanged
    {
        #region Properties

        private TreeNode? node;

        public TreeNode? Node
        {
            get => node;
            internal set
            {
                node = value;
                OnPropertyChanged("Node");
            }
        }

        #endregion Properties

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Node != null)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        e.Handled = true;
                        if (!Node.IsExpanded)
                        {
                            Node.IsExpanded = true;
                            ChangeFocus(Node);
                        }
                        else if (Node.Children.Count > 0)
                            ChangeFocus(Node.Children[0]);
                        break;

                    case Key.Left:

                        e.Handled = true;
                        if (Node.IsExpanded && Node.IsExpandable)
                        {
                            Node.IsExpanded = false;
                            ChangeFocus(Node);
                        }
                        else
                            ChangeFocus(Node.Parent);
                        break;

                    case Key.Subtract:
                        e.Handled = true;
                        Node.IsExpanded = false;
                        ChangeFocus(Node);
                        break;

                    case Key.Add:
                        e.Handled = true;
                        Node.IsExpanded = true;
                        ChangeFocus(Node);
                        break;

                    case Key.Delete:
                        e.Handled = true;
                        Node.Delete();
                        break;
                }
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        private void ChangeFocus(TreeNode node)
        {
            var tree = node.Tree;
            if (tree != null)
            {
                if (tree.ItemContainerGenerator.ContainerFromItem(node) is TreeListItem item)
                    item.Focus();
                else
                    tree.PendingFocusNode = node;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion INotifyPropertyChanged Members
    }
}