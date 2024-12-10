using System;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Shortcuts
{
    public class SpecialPageShortcutHandler<T> : ShortcutHandler<T> where T : IShortcut
    {
        private readonly string title;
        private readonly string tag;
        private readonly Action action;
        private readonly T shortcutInstance;

        public SpecialPageShortcutHandler(string title, string tag, Action action, T shortcutInstance)
        {
            this.title = title;
            this.tag = tag;
            this.action = action;
            this.shortcutInstance = shortcutInstance;
        }

        protected override Uri DoGetUri(T shortcut)
        {
            return new Uri("hercules:" + tag, UriKind.RelativeOrAbsolute);
        }

        protected override bool DoOpen(T shortcut)
        {
            action();
            return true;
        }

        protected override string DoGetTitle(T shortcut) => title;

        protected override string DoGetIcon(T shortcut) => Fugue.Icons.BlueDocument;

        protected override bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out T shortcut)
        {
            if (string.IsNullOrEmpty(uri.Authority) && uri.Segments.Length == 1 && uri.LocalPath == tag)
            {
                shortcut = shortcutInstance;
                return true;
            }
            shortcut = default!;
            return false;
        }
    }
}
