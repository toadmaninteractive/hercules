using Anthropic.SDK;
using Hercules.Shell;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiSettingsTab : PageTab
    {
        public ObservableCollection<string> AiModels { get; private set; } = new();
        public AiSettings Settings { get; }
        public ICommand RefreshModelsCommand { get; }

        public AiSettingsTab(AiSettings settings)
        {
            Title = "AI";
            Settings = settings;
            RefreshModelsCommand = Commands.ExecuteAsync(RefreshModels);
        }

        public override void OnActivate()
        {
            if (!AiModels.Contains(Settings.AiModel.Value))
                AiModels.Add(Settings.AiModel.Value);
            RefreshModels().Track();
        }

        private async Task RefreshModels()
        {
            using var anthropicClient = new AnthropicClient(new APIAuthentication(Settings.AnthropicApiKey.Value));
            var response = await anthropicClient.Models.ListModelsAsync();
            AiModels.SynchronizeOrdered(response.Models.ConvertAll(m => m.Id));
        }
    }
}
