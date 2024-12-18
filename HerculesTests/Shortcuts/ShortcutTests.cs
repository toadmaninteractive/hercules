using Hercules.ApplicationUpdate;
using Hercules.Documents;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace Hercules.Shortcuts.Tests
{
    [TestFixture]
    public class ShortcutTests
    {
        [Test]
        public void ReleaseNotesTest()
        {
            var shortcutService = new ShortcutService();
            shortcutService.RegisterSpecialPage("Release Notes", "release_notes", () => { }, ReleaseNotesShortcut.Instance);
            var uri = new Uri("hercules:release_notes", UriKind.RelativeOrAbsolute);
            ClassicAssert.IsTrue(shortcutService.TryParseUri(uri, out var shortcut));
            ClassicAssert.IsInstanceOf<ReleaseNotesShortcut>(shortcut);
            ClassicAssert.AreEqual(uri, shortcutService.ToUri(shortcut!));
        }

        [Test]
        public void LocalDocumentTest()
        {
            var shortcutService = new ShortcutService();
            var project = new ObservableValue<Project?>(null);
            shortcutService.RegisterHandler(new DocumentShortcutHandler(project, Commands.Execute<IDocument>(_ => { })));
            ClassicAssert.IsTrue(shortcutService.TryParseUri(new Uri("hercules:////document_name"), out var shortcut));
            ClassicAssert.IsInstanceOf<DocumentShortcut>(shortcut);
            ClassicAssert.AreEqual("document_name", ((DocumentShortcut)shortcut!).DocumentId);
        }
    }
}
