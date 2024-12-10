namespace Hercules.Shortcuts
{
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IShortcut
    {
    }
#pragma warning restore CA1040 // Avoid empty interfaces

    public interface IShortcutProvider
    {
        IShortcut Shortcut { get; }
    }

    public record DocumentShortcut(string DocumentId) : IShortcut;
}
