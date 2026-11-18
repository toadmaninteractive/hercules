using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class AdviceManager : NotifyPropertyChanged
    {
        public ObservableCollection<Advice> Advices { get; } = new();

        public bool NewAdvice
        {
            get;
            set => SetField(ref field, value);
        }

        public void RemoveByType(string type)
        {
            Advices.RemoveAll(advice => advice.Type == type);
        }

        public void AddAdvice(string type, string title, ICommand command)
        {
            Advices.Add(new Advice(type, title, command));
        }
    }
}
