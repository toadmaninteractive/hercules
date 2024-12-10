using System.Windows;

namespace Hercules.Controls
{
    public abstract class CartesianRenderer : FrameworkElement
    {
        protected CartesianPanel Panel => (CartesianPanel)VisualParent;

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return availableSize;
        }
    }
}
