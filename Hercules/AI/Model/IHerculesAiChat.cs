using System.Collections.Generic;
using System.Threading;

namespace Hercules.AI
{
    public interface IHerculesAiChat
    {
        void Ask(string userPrompt, IReadOnlyCollection<string> attachments, CancellationToken ct);
        void Reset();
    }
}
