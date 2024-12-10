using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Hercules.Controls.Tree
{
    public sealed class TreeNode : NotifyPropertyChanged
    {
        #region NodeCollection

        private class NodeCollection : Collection<TreeNode>
        {
            private readonly TreeNode owner;

            public NodeCollection(TreeNode owner)
            {
                this.owner = owner;
            }

            protected override void ClearItems()
            {
                while (this.Count != 0)
                    this.RemoveAt(this.Count - 1);
            }

            protected override void InsertItem(int index, TreeNode item)
            {
                ArgumentNullException.ThrowIfNull(nameof(item));

                if (item.Parent != owner)
                {
                    item.Parent?.Children.Remove(item);
                    item.Parent = owner;
                    item.Index = index;
                    for (int i = index; i < Count; i++)
                        this[i].Index++;
                    base.InsertItem(index, item);
                }
            }

            protected override void RemoveItem(int index)
            {
                TreeNode item = this[index];
                item.Parent = null;
                item.Index = -1;
                for (int i = index + 1; i < Count; i++)
                    this[i].Index--;
                base.RemoveItem(index);
            }

            protected override void SetItem(int index, TreeNode item)
            {
                ArgumentNullException.ThrowIfNull(nameof(item));
                RemoveAt(index);
                InsertItem(index, item);
            }
        }

        #endregion NodeCollection

        internal TreeList Tree { get; }

        private INotifyCollectionChanged? childrenSource;

        internal INotifyCollectionChanged? ChildrenSource
        {
            get => childrenSource;
            set
            {
                if (childrenSource != null)
                    childrenSource.CollectionChanged -= ChildrenChanged;

                childrenSource = value;

                if (childrenSource != null)
                    childrenSource.CollectionChanged += ChildrenChanged;
            }
        }

        public int Index { get; private set; } = -1;

        /// <summary>
        /// Returns true if all parent nodes of this node are expanded.
        /// </summary>
        internal bool IsVisible
        {
            get
            {
                TreeNode? node = Parent;
                while (node != null)
                {
                    if (!node.IsExpanded)
                        return false;
                    node = node.Parent;
                }
                return true;
            }
        }

        public bool IsExpandedOnce { get; internal set; }

        public bool HasChildren { get; internal set; }

        private bool isExpanded;

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value != IsExpanded)
                {
                    Tree.SetIsExpanded(this, value);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsExpandable));
                }
            }
        }

        internal void AssignIsExpanded(bool value)
        {
            isExpanded = value;
        }

        public bool IsExpandable => (HasChildren && !IsExpandedOnce) || Nodes.Count > 0;

        private bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set => SetField(ref isSelected, value);
        }

        public TreeNode? Parent { get; private set; }

        public int Level
        {
            get
            {
                if (Parent == null)
                    return -1;
                else
                    return Parent.Level + 1;
            }
        }

        public TreeNode? PreviousNode
        {
            get
            {
                if (Parent != null)
                {
                    int index = Index;
                    if (index > 0)
                        return Parent.Nodes[index - 1];
                }
                return null;
            }
        }

        public TreeNode? NextNode
        {
            get
            {
                if (Parent != null)
                {
                    int index = Index;
                    if (index < Parent.Nodes.Count - 1)
                        return Parent.Nodes[index + 1];
                }
                return null;
            }
        }

        internal TreeNode? BottomNode
        {
            get
            {
                TreeNode? parent = this.Parent;
                if (parent != null)
                {
                    if (parent.NextNode != null)
                        return parent.NextNode;
                    else
                        return parent.BottomNode;
                }
                return null;
            }
        }

        internal TreeNode? NextVisibleNode
        {
            get
            {
                if (IsExpanded && Nodes.Count > 0)
                    return Nodes[0];
                else
                {
                    TreeNode? nn = NextNode;
                    if (nn != null)
                        return nn;
                    else
                        return BottomNode;
                }
            }
        }

        public int VisibleChildrenCount => AllVisibleChildren.Count();

        public IEnumerable<TreeNode> AllVisibleChildren
        {
            get
            {
                int level = this.Level;
                TreeNode? node = this;
                while (true)
                {
                    node = node.NextVisibleNode;
                    if (node != null && node.Level > level)
                        yield return node;
                    else
                        break;
                }
            }
        }

        public object? Tag { get; }

        internal Collection<TreeNode> Children { get; }

        public ReadOnlyCollection<TreeNode> Nodes { get; }

        internal TreeNode(TreeList tree, object? tag)
        {
            ArgumentNullException.ThrowIfNull(nameof(tree));

            this.Tree = tree;
            this.Children = new NodeCollection(this);
            this.Nodes = new ReadOnlyCollection<TreeNode>(Children);
            this.Tag = tag;
        }

        public override string? ToString()
        {
            if (Tag != null)
                return Tag.ToString();
            else
                return base.ToString();
        }

        void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        int index = e.NewStartingIndex;
                        int rowIndex = Tree.Rows.IndexOf(this);
                        foreach (object? obj in e.NewItems)
                        {
                            Tree.InsertNewNode(this, obj, rowIndex, index);
                            index++;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (Children.Count > e.OldStartingIndex)
                        RemoveChildAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    while (Children.Count > 0)
                        RemoveChildAt(0);
                    Tree.CreateChildrenNodes(this);
                    break;
            }
            HasChildren = Children.Count > 0;
            RaisePropertyChanged(nameof(IsExpandable));
        }

        private void RemoveChildAt(int index)
        {
            var child = Children[index];
            Tree.DropChildrenRows(child, true);
            ClearChildrenSource(child);
            Children.RemoveAt(index);
        }

        private void ClearChildrenSource(TreeNode node)
        {
            node.ChildrenSource = null;
            foreach (var n in node.Children)
                ClearChildrenSource(n);
        }

        public void Delete()
        {
            Parent?.RemoveChildAt(Index);
        }
    }
}
