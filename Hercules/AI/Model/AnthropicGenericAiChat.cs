using Anthropic.SDK;
using System;

namespace Hercules.AI
{
    public class AnthropicGenericAiChat : GenericAiChat
    {
        public AnthropicGenericAiChat(IAiChatLog chatLog, AiTools aiTools, AiSettings settings, ObservableValue<bool> isGenerating)
            : base(new AnthropicClient(settings.AnthropicApiKey.Value).Messages, chatLog, aiTools, settings, isGenerating)
        {
            chatOptions.ModelId = settings.AiModel.Value;
            chatOptions.MaxOutputTokens = 2048;
        }
    }
}
