using Json;
using System;
using System.Collections.Generic;

namespace Hercules.AI
{
    public interface IAiChatLog
    {
        event Action OnChanged;

        void AddAiMessage(string message);
        void AddUserMessage(string message);
        void AddToolCall(string function, IReadOnlyCollection<KeyValuePair<string, ImmutableJson>> arguments, string response);
        void AddException(Exception exception);
        void AddSpecialMessage(string message);
    }
}
