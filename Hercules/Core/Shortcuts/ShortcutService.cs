using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Shortcuts
{
    public class ShortcutService
    {
        readonly Dictionary<Type, IShortcutHandler> shortcutHandlers = new();

        public IEnumerable<IShortcutHandler> AllHandlers => shortcutHandlers.Values;

        public void RegisterHandler(IShortcutHandler handler)
        {
            shortcutHandlers.Add(handler.Type, handler);
        }

        public void RegisterSpecialPage<T>(string title, string tag, Action action, T shortcutInstance) where T : IShortcut
        {
            RegisterHandler(new SpecialPageShortcutHandler<T>(title, tag, action, shortcutInstance));
        }

        public IShortcutHandler GetHandler<T>() where T : IShortcut
        {
            return GetHandler(typeof(T));
        }

        public IShortcutHandler GetHandler(Type type)
        {
            return shortcutHandlers[type];
        }

        public IShortcutHandler GetHandler(IShortcut shortcut)
        {
            return GetHandler(shortcut.GetType());
        }

        public bool Open(IShortcut shortcut)
        {
            return GetHandler(shortcut).Open(shortcut);
        }

        public Uri ToUri(IShortcut shortcut)
        {
            return GetHandler(shortcut).GetUri(shortcut);
        }

        public bool TryParseUri(Uri uri, [MaybeNullWhen(false)] out IShortcut shortcut)
        {
            foreach (var handler in shortcutHandlers)
            {
                if (handler.Value.TryParseUri(uri, out var result))
                {
                    shortcut = result;
                    return true;
                }
            }

            shortcut = default!;
            return false;
        }
    }
}
