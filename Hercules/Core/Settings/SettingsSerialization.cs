using System;
using System.Diagnostics.CodeAnalysis;

namespace Hercules
{
    public interface ISettingsReader : IDisposable
    {
        bool Read<T>(string name, [MaybeNullWhen(returnValue: false)] out T value);
    }

    public interface ISettingsWriter
    {
        void Write<T>(string name, T value);
    }

    public class EmptySettingsReader : ISettingsReader
    {
        public bool Read<T>(string name, [MaybeNullWhen(returnValue: false)] out T value)
        {
            value = default!;
            return false;
        }

        public void Dispose()
        {
        }

        public static readonly ISettingsReader Default = new EmptySettingsReader();
    }
}
