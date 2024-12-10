using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Shortcuts
{
    public interface IShortcutHandler
    {
        Type Type { get; }

        string GetTitle(IShortcut shortcut);

        bool TryParseUri(Uri uri, [MaybeNullWhen(false)] out IShortcut shortcut);

        Uri GetUri(IShortcut shortcut);

        string GetIcon(IShortcut shortcut);

        bool Open(IShortcut shortcut);

        bool IsFolder { get; }

        /// <summary>
        /// List of nested items in the folder. Not null when is IsFolder is true.
        /// </summary>
        /// <param name="shortcut">Shortcut folder</param>
        /// <returns>List of nested items in the folder</returns>
        IEnumerable? GetItems(IShortcut shortcut);

        IObservable<IShortcutHandler>? OnChange { get; }
    }

    public abstract class ShortcutHandler<T> : IShortcutHandler where T : IShortcut
    {
        public Type Type => typeof(T);

        public string GetTitle(IShortcut shortcut)
        {
            return DoGetTitle(ValidateShortcut(shortcut));
        }

        public virtual bool TryParseUri(Uri uri, [MaybeNullWhen(false)] out IShortcut shortcut)
        {
            var result = DoTryParseUri(uri, out var s);
            shortcut = s!;
            return result;
        }

        public Uri GetUri(IShortcut shortcut)
        {
            return DoGetUri(ValidateShortcut(shortcut));
        }

        public string GetIcon(IShortcut shortcut)
        {
            return DoGetIcon(ValidateShortcut(shortcut));
        }

        public bool Open(IShortcut shortcut)
        {
            return DoOpen(ValidateShortcut(shortcut));
        }

        public IEnumerable? GetItems(IShortcut shortcut)
        {
            return DoGetItems(ValidateShortcut(shortcut));
        }

        public virtual bool IsFolder => false;

        private static T ValidateShortcut(IShortcut shortcut)
        {
            ArgumentNullException.ThrowIfNull(nameof(shortcut));
            if (!(shortcut is T))
                throw new ArgumentOutOfRangeException(nameof(shortcut));
            return (T)shortcut;
        }

        protected abstract string DoGetIcon(T shortcut);

        protected abstract bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out T shortcut);

        protected abstract Uri DoGetUri(T shortcut);

        protected abstract string DoGetTitle(T shortcut);

        protected abstract bool DoOpen(T shortcut);

        protected virtual IEnumerable? DoGetItems(T shortcut) => null;

        public virtual IObservable<IShortcutHandler>? OnChange => null;
    }
}
