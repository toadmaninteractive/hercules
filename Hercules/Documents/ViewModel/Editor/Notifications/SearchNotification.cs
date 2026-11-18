using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class SearchNotification : Notification
    {

        public string Text
        {
            get;
            set => SetField(ref field, value);
        } = string.Empty;

        public bool WholeWord
        {
            get;
            set => SetField(ref field, value);
        }

        public bool MatchCase
        {
            get;
            set => SetField(ref field, value);
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
