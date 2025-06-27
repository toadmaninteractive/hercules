using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public interface IHerculesAiChat
    {
        bool IsConnected { get; }
        void Init();
        void Ask(string userPrompt, IReadOnlyCollection<string> attachments, CancellationToken ct);
        void Reset();
    }
}
