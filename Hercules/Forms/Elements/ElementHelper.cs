namespace Hercules.Forms.Elements
{
    public static class ElementHelper
    {
        public static void ExpandSubtree(this Element root)
        {
            root.Form.Run(transaction => SetExpandedStateRecursively(root, true, transaction));
        }

        public static void CollapseSubtree(this Element root)
        {
            root.Form.Run(transaction => SetExpandedStateRecursively(root, false, transaction));
        }

        public static void ExpandInitially(this Element root, ITransaction transaction)
        {
            switch (root.Form.Settings.ExpandNewElement.Value)
            {
                case ExpandElementType.Expand:
                    ExpandElement(root, transaction);
                    break;
                case ExpandElementType.ExpandTree:
                    SetExpandedStateRecursively(root, true, transaction);
                    break;
            }
        }

        public static T? GetParentByType<T>(this Element? element) where T : class
        {
            if (element == null)
                return null;
            if (element is T t)
                return t;
            return GetParentByType<T>(element.Parent as Element);
        }

        public static void SetExpandedStateRecursively(this Element root, bool isExpanded, ITransaction transaction)
        {
            root.Visit(
                element =>
                {
                    if (element is IExpandableElement expandableElement)
                        expandableElement.Expand(isExpanded, transaction, false);
                }, VisitOptions.ChildrenFirst);
        }

        private static void ExpandElement(Element root, ITransaction transaction)
        {
            IExpandableElement? exp = null;
            root.Visit(element =>
            {
                if (element is IExpandableElement expandableElement)
                {
                    exp = expandableElement;
                }
            }, VisitOptions.ChildrenFirst);
            exp?.Expand(true, transaction, false);
        }
    }
}
