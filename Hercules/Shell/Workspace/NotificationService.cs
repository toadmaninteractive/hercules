using System.Collections.ObjectModel;

namespace Hercules.Shell
{
    public class NotificationService
    {
        public ObservableCollection<Notification> Items { get; } = new();

        public void Show(Notification notificationViewModel)
        {
            notificationViewModel.Owner = this;
            Items.Add(notificationViewModel);
        }

        public void Remove(Notification notificationViewModel)
        {
            Items.Remove(notificationViewModel);
        }

        public void RemoveAll<T>() where T : Notification
        {
            Items.RemoveAll(n => n is T);
        }

        public void RemoveBySource(object source)
        {
            Items.RemoveAll(n => n.Source == source);
        }

        public void AddMessage(string message, MessageNotificationType type, object? source = null)
        {
            Show(new MessageNotification(message, type, source));
        }
    }
}
