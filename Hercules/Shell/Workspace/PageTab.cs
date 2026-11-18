using System.Windows.Input;

namespace Hercules.Shell
{
    public class PageTab : NotifyPropertyChanged
    {

        public string? Title
        {
            get;
            set => SetField(ref field, value);
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
