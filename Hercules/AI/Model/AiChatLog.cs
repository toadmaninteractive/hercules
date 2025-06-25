using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Hercules.AI
{
    public interface IAiChatLog
    {
        event Action OnChanged;

        void AddAiMessage(string message);
        void AddUserMessage(string message);
        void AddToolCall(string function, string arguments, string response);
        void AddException(Exception exception);
        void AddSpecialMessage(string message);
    }

    public class AiChatLog : ViewModelBase, IAiChatLog
    {
        public FlowDocument Document { get; } = new();

        public event Action? OnChanged;

        public void AddAiMessage(string message)
        {
            Document.Blocks.Add(new Paragraph(new Run(message)));
            OnChanged?.Invoke();
        }

        public void AddException(Exception exception)
        {
            var errorParagraph = new Paragraph(new Run(exception.Message.Trim())) { Foreground = Brushes.Red };
            Document.Blocks.Add(errorParagraph);
            OnChanged?.Invoke();
        }

        public void AddToolCall(string function, string arguments, string response)
        {
            var glyphRun = new Run("+ ");
            var functionRun = new Run(function);
            var hyperlink = new Hyperlink(glyphRun);
            var expandedSection = new Span();
            hyperlink.TextDecorations = new TextDecorationCollection();
            hyperlink.Cursor = Cursors.Hand;
            var paragraph = new Paragraph { Foreground = Brushes.Gray, FontWeight = FontWeights.Light, FontFamily = new FontFamily("Courier New"), FontSize = 11 };
            paragraph.Inlines.Add(hyperlink);
            paragraph.Inlines.Add(functionRun);
            expandedSection.Inlines.Add(new LineBreak());
            expandedSection.Inlines.Add(new LineBreak());
            expandedSection.Inlines.Add(new Run(arguments));
            expandedSection.Inlines.Add(new LineBreak());
            expandedSection.Inlines.Add(new LineBreak());
            expandedSection.Inlines.Add(new Run(response));
            hyperlink.Command = Commands.Execute(() =>
            {
                bool isExpanded = paragraph.Inlines.Contains(expandedSection);
                if (isExpanded)
                {
                    glyphRun.Text = "+ ";
                    paragraph.Inlines.Remove(expandedSection);
                }
                else
                {
                    glyphRun.Text = "- ";
                    paragraph.Inlines.Add(expandedSection);
                }
            });
            Document.Blocks.Add(paragraph);
            OnChanged?.Invoke();
        }

        public void AddSpecialMessage(string message)
        {
            Document.Blocks.Add(new Paragraph(new Run(message)) { Foreground = Brushes.Yellow });
            OnChanged?.Invoke();
        }

        public void AddUserMessage(string message)
        {
            var paragraph = new Paragraph(new Run(message)) { FontStyle = FontStyles.Italic, Foreground = Brushes.DarkBlue };
            Document.Blocks.Add(paragraph);
            OnChanged?.Invoke();
        }

        public void Clear()
        {
            Document.Blocks.Clear();
        }
    }
}
