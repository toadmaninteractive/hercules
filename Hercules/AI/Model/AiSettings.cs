using Anthropic.SDK.Constants;
using System;
using System.Collections.Generic;

namespace Hercules.AI
{
    public class AiSettings : ISettingGroup
    {
        public Setting<string> AnthropicApiKey { get; } = new Setting<string>(nameof(AnthropicApiKey), "");
        public Setting<string> AiModel { get; } = new Setting<string>(nameof(AiModel), AnthropicModels.Claude35Sonnet);
        public Setting<double> AiTemperature { get; } = new Setting<double>(nameof(AiTemperature), 0.5);

        public IEnumerable<ISetting> GetSettings()
        {
            yield return AnthropicApiKey;
            yield return AiModel;
            yield return AiTemperature;
        }

    }
}
