using System;

namespace Hercules
{
    public interface ICommandContext
    {
        object? GetCommandParameter(Type type);
    }
}
