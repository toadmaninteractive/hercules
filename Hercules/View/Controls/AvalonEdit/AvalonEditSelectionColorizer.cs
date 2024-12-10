using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Hercules.Controls.AvalonEdit
{
    public class HighlightSelectedIdentifierColorizer : DocumentColorizingTransformer
    {
        private readonly TextEditor textEditor;

        public HighlightSelectedIdentifierColorizer(TextEditor textEditor)
        {
            this.textEditor = textEditor;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var selection = textEditor.SelectedText;

            if (string.IsNullOrEmpty(textEditor.SelectedText))
                return;

            Match match = Regex.Match(textEditor.SelectedText, @"^[a-zA-Z]{1}[a-zA-Z0-9_]*$");
            if (!match.Success)
                return;

            bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

            if (textEditor.SelectionStart > 0 && IsIdentChar(CurrentContext.Document.GetCharAt(textEditor.SelectionStart - 1)))
                return;

            var selectionEnd = textEditor.SelectionStart + textEditor.SelectionLength;
            if (selectionEnd < CurrentContext.Document.TextLength && IsIdentChar(CurrentContext.Document.GetCharAt(selectionEnd)))
                return;

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            while ((index = text.IndexOf(selection, start, StringComparison.Ordinal)) >= 0)
            {
                ChangeLinePart(lineStartOffset + index, lineStartOffset + index + selection.Length, element => element.BackgroundBrush = Brushes.Khaki);
                start = index + selection.Length;
            }
        }
    }
}
