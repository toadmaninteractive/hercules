using System.Windows.Input;

namespace Hercules.Shell
{
    public class AdviceOption : UiOption
    {
        public AdviceManager AdviceManager { get; }

        public ICommand MarkAsReadCommand { get; }

        public AdviceOption(AdviceManager adviceManager)
        {
            AdviceManager = adviceManager;
            MarkAsReadCommand = Commands.Execute(() => adviceManager.NewAdvice = false);
        }
    }
}
