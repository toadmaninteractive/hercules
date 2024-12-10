using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Hercules.Controls.Tree
{
    // This is a copy of Andrey Gliznetsov's WPF TreeListView Control implementation with cosmetic changes
    // https://www.codeproject.com/Articles/30721/WPF-TreeListView-Control
    public class TreeList : ListView
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(ITreeModel), typeof(TreeList), new PropertyMetadata(null, OnModelChanged));

        public ITreeModel? Model
        {
            get => (ITreeModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        #region Properties

        /// <summary>
        /// Internal collection of rows representing visible nodes, actually displayed in the ListView
        /// </summary>
        internal ObservableRangeCollection<TreeNode> Rows
        {
            get;
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeList target = (TreeList)d;
            target.OnModelChanged();
        }

        private void OnModelChanged()
        {
            Rows.Clear();
            root.Children.Clear();
            CreateChildrenNodes(root);
        }

        private readonly TreeNode root;

        internal TreeNode Root => root;

        public ReadOnlyCollection<TreeNode> Nodes => Root.Nodes;

        internal TreeNode? PendingFocusNode { get; set; }

        public ICollection<TreeNode> SelectedNodes => SelectedItems.Cast<TreeNode>().ToArray();

        public TreeNode? SelectedNode
        {
            get
            {
                if (SelectedItems.Count > 0)
                    return SelectedItems[0] as TreeNode;
                else
                    return null;
            }
        }

        #endregion Properties

        public TreeList()
        {
            Rows = new ObservableRangeCollection<TreeNode>();
            root = new TreeNode(this, null) { IsExpanded = true };
            ItemsSource = Rows;
            ItemContainerGenerator.StatusChanged += ItemContainerGeneratorStatusChanged;
        }

        void ItemContainerGeneratorStatusChanged(object? sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && PendingFocusNode != null)
            {
                var item = ItemContainerGenerator.ContainerFromItem(PendingFocusNode) as TreeListItem;
                item?.Focus();
                PendingFocusNode = null;
            }
        }

        protected override DependencyObject GetContainerForItemOverride() => new TreeListItem();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeListItem;

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is TreeListItem ti && item is TreeNode node)
            {
                ti.Node = node;
                base.PrepareContainerForItemOverride(element, node.Tag);
            }
        }

        internal void SetIsExpanded(TreeNode node, bool value)
        {
            if (value)
            {
                if (!node.IsExpandedOnce)
                {
                    node.IsExpandedOnce = true;
                    node.AssignIsExpanded(value);
                    CreateChildrenNodes(node);
                }
                else
                {
                    node.AssignIsExpanded(value);
                    CreateChildrenRows(node);
                }
            }
            else
            {
                DropChildrenRows(node, false);
                node.AssignIsExpanded(value);
            }
        }

        internal void CreateChildrenNodes(TreeNode node)
        {
            var children = GetChildren(node);
            if (children != null)
            {
                int rowIndex = Rows.IndexOf(node);
                node.ChildrenSource = children as INotifyCollectionChanged;
                foreach (object? obj in children)
                {
                    TreeNode child = new TreeNode(this, obj);
                    child.HasChildren = HasChildren(child);
                    node.Children.Add(child);
                }
                Rows.InsertRange(rowIndex + 1, node.Children);
            }
        }

        private void CreateChildrenRows(TreeNode node)
        {
            int index = Rows.IndexOf(node);
            if (index >= 0 || node == root) // ignore invisible nodes
            {
                var nodes = node.AllVisibleChildren;
                Rows.InsertRange(index + 1, nodes);
            }
        }

        internal void DropChildrenRows(TreeNode node, bool removeParent)
        {
            int start = Rows.IndexOf(node);
            if (start >= 0 || node == root) // ignore invisible nodes
            {
                int count = node.VisibleChildrenCount;
                if (removeParent)
                    count++;
                else
                    start++;
                Rows.RemoveRange(start, count);
            }
        }

        private IEnumerable? GetChildren(TreeNode parent)
        {
            return Model?.GetChildren(parent.Tag);
        }

        private bool HasChildren(TreeNode parent)
        {
            if (parent == Root)
                return true;
            else if (Model != null)
                return Model.HasChildren(parent.Tag);
            else
                return false;
        }

        internal void InsertNewNode(TreeNode parent, object? tag, int rowIndex, int index)
        {
            TreeNode node = new TreeNode(this, tag);
            if (index >= 0 && index < parent.Children.Count)
                parent.Children.Insert(index, node);
            else
            {
                index = parent.Children.Count;
                parent.Children.Add(node);
            }
            Rows.Insert(rowIndex + index + 1, node);
        }
    }
}
