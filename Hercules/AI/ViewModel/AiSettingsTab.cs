using Anthropic.SDK.Constants;
using Hercules.Shell;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiSettingsTab : PageTab
    {
        public List<string> AiModels { get; }
        public AiSettings Settings { get; }

        public AiSettingsTab(AiSettings settings)
        {
            Title = "AI";
            AiModels = new List<string>
            {
                AnthropicModels.Claude3Opus,
                AnthropicModels.Claude3Sonnet,
                AnthropicModels.Claude35Sonnet,
                AnthropicModels.Claude37Sonnet,
                AnthropicModels.Claude4Sonnet,
                AnthropicModels.Claude4Opus,
                AnthropicModels.Claude35Haiku,
                AnthropicModels.Claude3Haiku,
            };
            Settings = settings;
        }
    }
}
