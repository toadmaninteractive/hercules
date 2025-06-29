using OllamaSharp;
using System;

namespace Hercules.AI
{
    public class OllamaAiChat : GenericAiChat
    {
        public OllamaAiChat(IAiChatLog chatLog, AiTools aiTools, AiSettings settings, ObservableValue<bool> isGenerating)
            : base(new OllamaApiClient(new Uri(settings.OllamaUri.Value), settings.OllamaModel.Value), chatLog, aiTools, settings, isGenerating)
        {
        }
    }
}
