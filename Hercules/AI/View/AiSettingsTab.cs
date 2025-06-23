using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiSettingsTab : PageTab
    {
        public Setting<string> AnthropicApiKey { get; }

        public AiSettingsTab(Setting<string> anthropicApiKey)
        {
            this.AnthropicApiKey = anthropicApiKey;
            this.Title = "AI";
        }
    }
}
