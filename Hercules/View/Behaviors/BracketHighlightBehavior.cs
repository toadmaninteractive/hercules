using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System;

namespace Hercules.Controls
{
    public class BracketHighlightBehavior : Behavior<TextEditor>
    {
        JavaScriptBracketSearcher? bracketSearcher;
        BracketHighlightRenderer? bracketRenderer;

        protected override void OnAttached()
        {
            bracketSearcher = new JavaScriptBracketSearcher();
            bracketRenderer = new BracketHighlightRenderer(AssociatedObject.TextArea.TextView);
            AssociatedObject.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
            bracketSearcher = null;
            bracketRenderer = null;
        }

        void Caret_PositionChanged(object? sender, EventArgs e)
        {
            var bracketSearchResult = bracketSearcher!.SearchBracket(this.AssociatedObject.Document, this.AssociatedObject.TextArea.Caret.Offset);
            this.bracketRenderer!.SetHighlight(bracketSearchResult);
        }
    }
}
