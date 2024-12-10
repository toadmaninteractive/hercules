using System.Windows.Input;

namespace Hercules.Shell
{
    public class Advice : NotifyPropertyChanged
    {
        public string Title { get; }
        public ICommand Command { get; }
        public string Type { get; }

        public Advice(string type, string title, ICommand command)
        {
            this.Type = type;
            this.Title = title;
            this.Command = command;
        }
    }
}
