using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;

namespace Hercules.Controls
{
    public class AvalonEditBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty CurrentLineProperty = DependencyProperty.Register("CurrentLine", typeof(int), typeof(AvalonEditBehavior), new PropertyMetadata((obj, args) =>
        {
            AvalonEditBehavior target = (AvalonEditBehavior)obj;
            var val = (int)args.NewValue;
            if (target.AssociatedObject.TextArea.Caret.Line != val)
            {
                target.AssociatedObject.TextArea.Caret.Line = (int)args.NewValue;
                target.AssociatedObject.ScrollToLine(val);
            }
        }));

        public int CurrentLine
        {
            get => AssociatedObject.TextArea.Caret.Line;
            set => AssociatedObject.TextArea.Caret.Line = value;
        }

        protected override void OnAttached()
        {
            AssociatedObject.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
        }

        void Caret_PositionChanged(object? sender, EventArgs e)
        {
            SetCurrentValue(CurrentLineProperty, AssociatedObject.TextArea.Caret.Line);
        }
    }
}
