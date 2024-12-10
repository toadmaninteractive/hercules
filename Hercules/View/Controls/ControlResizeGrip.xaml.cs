using ICSharpCode.AvalonEdit;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Hercules.Controls
{
    /// <summary>
    /// Interaction logic for ControlResizeGrip.xaml
    /// </summary>
    public partial class ControlResizeGrip : Thumb
    {
        public ControlResizeGrip()
        {
            Focusable = false;
            InitializeComponent();
            DragDelta += Thumb_DragDelta;
            MouseDoubleClick += MouseDoubleClickHandler;
        }

        void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var control = (FrameworkElement)Parent;
            control.Height = Math.Max(control.MinHeight, control.Height + e.VerticalChange);
        }

        private void MouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            var control = Parent as System.Windows.Controls.Panel;
            if (control == null)
                return;

            var multilineTextEditor = LogicalTreeHelper.FindLogicalNode(control, "MultilineTextEditor");
            double extentHeight;
            if (multilineTextEditor is TextBoxBase textBoxBase)
                extentHeight = textBoxBase.ExtentHeight + 5;
            else if (multilineTextEditor is TextEditor editor)
                extentHeight = editor.ExtentHeight + 7;
            else return;

            if (control.Height < extentHeight)
                control.Height = extentHeight;
            else if (control.Height > extentHeight)
                control.Height = Math.Max(control.MinHeight, extentHeight);
        }
    }
}