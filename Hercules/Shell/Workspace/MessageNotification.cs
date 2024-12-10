using System.Windows.Media;

namespace Hercules.Shell
{
    public enum MessageNotificationType
    {
        Info,
        Warning,
        Error,
    }

    public class MessageNotification : Notification
    {
        public string Message { get; }

        public MessageNotificationType Type { get; }

        public Brush TextColor { get; }

        public string Icon { get; }

        public MessageNotification(string message, MessageNotificationType type, object? source)
            : base(source)
        {
            Type = type;
            Icon = GetIconByType(type);
            TextColor = GetBrushByType(type);
            Message = message;
        }

        private static string GetIconByType(MessageNotificationType type)
        {
            return type switch
            {
                MessageNotificationType.Error => Fugue.Icons.ExclamationRed,
                MessageNotificationType.Warning => Fugue.Icons.ExclamationFrame,
                _ => Fugue.Icons.Information
            };
        }

        private static Brush GetBrushByType(MessageNotificationType type)
        {
            return type switch
            {
                MessageNotificationType.Error => Brushes.Red,
                MessageNotificationType.Warning => Brushes.Chocolate,
                _ => Brushes.Black
            };
        }
    }
}
