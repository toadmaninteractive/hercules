using System;

namespace Hercules.Shortcuts
{
    public record SettingsShortcut(string? Group) : IShortcut
    {
        public static readonly SettingsShortcut Instance = new(Group: null);
        public static readonly SettingsShortcut DocumentEditor = new("Document Editor");
    }

    public class SettingsShortcutHandler : ShortcutHandler<SettingsShortcut>
    {
        private readonly Action<string?> showSettingsAction;

        public SettingsShortcutHandler(Action<string?> showSettingsAction)
        {
            this.showSettingsAction = showSettingsAction;
        }

        protected override string DoGetIcon(SettingsShortcut shortcut) => Fugue.Icons.Gear;

        protected override bool DoTryParseUri(Uri uri, out SettingsShortcut shortcut)
        {
            if (string.IsNullOrEmpty(uri.Authority) && uri.Segments.Length == 1 && uri.LocalPath == "settings")
            {
                shortcut = new SettingsShortcut(Uri.UnescapeDataString(uri.Fragment));
                return true;
            }
            shortcut = default!;
            return false;
        }

        protected override Uri DoGetUri(SettingsShortcut shortcut)
        {
            if (shortcut.Group == null)
                return new Uri("hercules:settings", UriKind.RelativeOrAbsolute);
            else
                return new Uri($"hercules:settings#{shortcut.Group}", UriKind.RelativeOrAbsolute);
        }

        protected override string DoGetTitle(SettingsShortcut shortcut) => "Settings";

        protected override bool DoOpen(SettingsShortcut shortcut)
        {
            showSettingsAction(shortcut.Group);
            return true;
        }
    }
}
