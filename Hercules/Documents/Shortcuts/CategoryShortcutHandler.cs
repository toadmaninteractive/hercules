using Hercules.Shortcuts;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace Hercules.Documents
{
    public class CategoryShortcutHandler : ShortcutHandler<CategoryShortcut>
    {
        private readonly IReadOnlyObservableValue<Project?> project;

        public CategoryShortcutHandler(IReadOnlyObservableValue<Project?> project)
        {
            this.project = project;
        }

        protected override Uri DoGetUri(CategoryShortcut shortcut)
        {
            return new Uri("hercules:////?category=" + shortcut.Category, UriKind.RelativeOrAbsolute);
        }

        protected override bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out CategoryShortcut shortcut)
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string? cat = query.Get("category");
            if (cat != null)
            {
                shortcut = new CategoryShortcut(cat);
                return true;
            }
            else
            {
                shortcut = default!;
                return false;
            }
        }

        protected override bool DoOpen(CategoryShortcut shortcut) => false;

        protected override string DoGetTitle(CategoryShortcut shortcut)
        {
            return shortcut.Category;
        }

        protected override string DoGetIcon(CategoryShortcut shortcut)
        {
            return Fugue.Icons.BlueFolder;
        }

        public override bool IsFolder => true;

        protected override IEnumerable? DoGetItems(CategoryShortcut shortcut)
        {
            return project.Value?.SchemafulDatabase.GetCategory(shortcut.Category)?.Documents;
        }

        public override IObservable<IShortcutHandler> OnChange
        {
            get { return project.Switch(p => p?.ObservableSchemafulDatabase).Select(p => this); }
        }
    }
}
