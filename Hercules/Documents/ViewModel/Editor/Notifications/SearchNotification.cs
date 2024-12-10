using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class SearchNotification : Notification
    {
        string text = string.Empty;

        public string Text
        {
            get => text;
            set => SetField(ref text, value);
        }

        bool wholeWord;

        public bool WholeWord
        {
            get => wholeWord;
            set => SetField(ref wholeWord, value);
        }

        bool matchCase;

        public bool MatchCase
        {
            get => matchCase;
            set => SetField(ref matchCase, value);
        }

        public SearchNotification(ICommand findCommand)
            : base(null)
        {
            FindCommand = findCommand;
        }

        public ICommand FindCommand { get; }

        public event Action? OnActivate;

        public void Activate()
        {
            OnActivate?.Invoke();
        }

        public bool HasContent()
        {
            return !string.IsNullOrEmpty(Text);
        }
    }
}
