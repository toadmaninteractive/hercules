using Anthropic.SDK.Constants;
using Hercules.Shell;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiSettingsTab : PageTab
    {
        public Setting<string> AnthropicApiKey { get; }
        public Setting<string> AiModel { get; }
        public List<string> AiModels { get; }

        public AiSettingsTab(Setting<string> anthropicApiKey, Setting<string> aiModel)
        {
            AnthropicApiKey = anthropicApiKey;
            AiModel = aiModel;
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
        }
    }
}
