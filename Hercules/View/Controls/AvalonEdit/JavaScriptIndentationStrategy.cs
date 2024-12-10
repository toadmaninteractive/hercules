using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;
using System;

namespace Hercules.Controls.AvalonEdit
{
    public class JavaScriptIndentationStrategy : DefaultIndentationStrategy
    {
        public string IndentationString { get; set; } = string.Empty;

        public override void IndentLine(TextDocument document, DocumentLine line)
        {
            DocumentLine prevLine = line.PreviousLine;
            if (prevLine == null)
                return;

            ISegment indentationSegment = TextUtilities.GetWhitespaceAfter(document, prevLine.Offset);
            string lastIndent = document.GetText(indentationSegment);

            string prevLineText = document.GetText(prevLine.Offset, prevLine.Length);
            string currentLineText = document.GetText(line.Offset, line.Length);

            char prevLastChar = GetLastChar(prevLineText);
            char currentFirstChar = GetFirstChar(currentLineText);

            string? indent = null;

            if (prevLastChar == '{')
            {
                indent = currentFirstChar == '}' ? lastIndent : lastIndent + IndentationString;
            }
            else if (currentFirstChar == '}')
            {
                indent = ReplaceFirst(lastIndent, IndentationString, "");
            }
            else if (prevLastChar == '[')
            {
                indent = currentFirstChar == ']' ? lastIndent : lastIndent + IndentationString;
            }
            else if (currentFirstChar == ']')
            {
                indent = ReplaceFirst(lastIndent, IndentationString, "");
            }

            if (indent != null)
            {
                indentationSegment = TextUtilities.GetWhitespaceAfter(document, line.Offset);
                document.Replace(indentationSegment, indent);
            }
            else
                base.IndentLine(document, line);
        }

        char GetLastChar(string text)
        {
            var trim = text.TrimEnd();
            if (trim.Length == 0)
                return (char)0;
            else
                return trim[^1];
        }

        char GetFirstChar(string text)
        {
            var trim = text.TrimStart();
            if (trim.Length == 0)
                return (char)0;
            else
                return trim[0];
        }

        string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
                return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
