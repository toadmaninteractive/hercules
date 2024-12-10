using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Indentation;
using Microsoft.Xaml.Behaviors;

namespace Hercules.Controls
{
    public class IndentationBehavior : Behavior<TextEditor>
    {
        protected JavaScriptIndentationStrategy? Strategy { get; private set; }

        protected override void OnAttached()
        {
            Strategy = new JavaScriptIndentationStrategy { IndentationString = AssociatedObject.TextArea.Options.IndentationString };
            AssociatedObject.TextArea.IndentationStrategy = Strategy;
            AssociatedObject.TextArea.TextEntered += TextArea_TextEntered;
        }

        void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == "}" || e.Text == "]")
            {
                var docLine = AssociatedObject.TextArea.Document.GetLineByNumber(AssociatedObject.TextArea.Caret.Line);
                Strategy!.IndentLine(AssociatedObject.TextArea.Document, docLine);
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
            AssociatedObject.TextArea.TextEntered -= TextArea_TextEntered;
            Strategy = null;
        }
    }
}
