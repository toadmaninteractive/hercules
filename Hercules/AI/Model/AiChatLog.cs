using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Hercules.AI
{
    public interface IAiChatLog
    {
        event Action OnChanged;

        void AddAiMessage(string message);
        void AddUserMessage(string message);
        void AddHerculesMessage(string message);
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

        public void AddHerculesMessage(string message)
        {
            Document.Blocks.Add(new Paragraph(new Run(message)) { Foreground = Brushes.Gray, FontWeight = FontWeights.Light, FontFamily = new FontFamily("Courier New"), FontSize = 11 });
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
