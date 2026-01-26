using Anthropic.SDK.Constants;
using System.Collections.Generic;

namespace Hercules.AI
{
    public enum AiModelProvider
    {
        Anthropic,
        Ollama,
    }

    public class AiSettings : ISettingGroup
    {
        public Setting<string> AnthropicApiKey { get; } = new Setting<string>(nameof(AnthropicApiKey), "");
        public Setting<string> AiModel { get; } = new Setting<string>(nameof(AiModel), AnthropicModels.Claude37Sonnet);
        public Setting<double> AiTemperature { get; } = new Setting<double>(nameof(AiTemperature), 0.5);
        public Setting<AiModelProvider> AiModelProvider { get; } = new Setting<AiModelProvider>(nameof(AiModelProvider), Hercules.AI.AiModelProvider.Anthropic);
        public Setting<string> OllamaUri { get; } = new Setting<string>(nameof(OllamaUri), "http://localhost:11434");
        public Setting<string> OllamaModel { get; } = new Setting<string>(nameof(OllamaModel), "llama3.1");
        public Setting<int> MaxTokens { get; } = new Setting<int>(nameof(MaxTokens), 4096);

        public IEnumerable<ISetting> GetSettings()
        {
            yield return AiModelProvider;
            yield return AnthropicApiKey;
            yield return AiModel;
            yield return AiTemperature;
            yield return OllamaUri;
            yield return OllamaModel;
        }

    }
}
