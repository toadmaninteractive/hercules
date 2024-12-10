using System.Windows.Input;

namespace Hercules.Shell
{
    public class PageTab : NotifyPropertyChanged
    {
        private string? title;

        public string? Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        public NotificationService Notifications { get; } = new NotificationService();
        public CommandBindingCollection RoutedCommandBindings { get; } = new CommandBindingCollection();

        public virtual void OnClose()
        {
        }

        public virtual void OnActivate()
        {
        }

        public virtual void OnDeactivate()
        {
        }
    }
}
