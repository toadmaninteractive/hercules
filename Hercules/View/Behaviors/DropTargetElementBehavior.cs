using Hercules.Forms.Elements;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace Hercules.Controls
{
    public class DropTargetElementBehavior : Behavior<FrameworkElement>
    {
        public double MaxHeight { get; set; }

        protected override void OnAttached()
        {
            AssociatedObject.Drop += Drop;
            AssociatedObject.DragEnter += DragEnter;
            AssociatedObject.DragOver += DragOver;
            AssociatedObject.DragLeave += DragLeave;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Drop -= Drop;
            AssociatedObject.DragEnter -= DragEnter;
            AssociatedObject.DragOver -= DragOver;
            AssociatedObject.DragLeave -= DragLeave;
        }

        IDropTargetElement DropTarget => (IDropTargetElement)AssociatedObject.DataContext;

        void DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(HerculesDragData.DragDataFormat))
            {
                var data = e.Data.GetData(HerculesDragData.DragDataFormat);
                if (e.OriginalSource == sender)
                    UpdateDropTarget(false, data);
            }
            e.Handled = true;
        }

        void DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(HerculesDragData.DragDataFormat))
            {
                var data = e.Data.GetData(HerculesDragData.DragDataFormat);
                var target = DropTarget;
                if (target != null && target.AllowDrop(data))
                {
                    var position = e.GetPosition(AssociatedObject);
                    if (MaxHeight > 0 && position.Y > MaxHeight)
                        UpdateDropTarget(false, data);
                    else
                    {
                        UpdateDropTarget(true, data);
                        e.Effects = DragDropEffects.All;
                    }
                }
            }
            e.Handled = true;
        }

        void DragEnter(object sender, DragEventArgs e)
        {
            DragOver(sender, e);
        }

        void Drop(object sender, DragEventArgs e)
        {
            var data = e.Data.GetData(HerculesDragData.DragDataFormat);
            DropTarget.Drop(data);
            e.Handled = true;
        }

        bool isTarget;

        void UpdateDropTarget(bool newIsTarget, object data)
        {
            if (isTarget != newIsTarget)
            {
                isTarget = newIsTarget;
                if (isTarget)
                    DropTarget.DragEnter(data);
                else
                    DropTarget.DragLeave(data);
            }
        }
    }
}
