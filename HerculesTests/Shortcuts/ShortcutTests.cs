using Hercules.ApplicationUpdate;
using Hercules.Documents;
using NUnit.Framework;
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
            Assert.IsTrue(shortcutService.TryParseUri(uri, out var shortcut));
            Assert.IsInstanceOf<ReleaseNotesShortcut>(shortcut);
            Assert.AreEqual(uri, shortcutService.ToUri(shortcut!));
        }

        [Test]
        public void LocalDocumentTest()
        {
            var shortcutService = new ShortcutService();
            var project = new ObservableValue<Project?>(null);
            shortcutService.RegisterHandler(new DocumentShortcutHandler(project, Commands.Execute<IDocument>(_ => { })));
            Assert.IsTrue(shortcutService.TryParseUri(new Uri("hercules:////document_name"), out var shortcut));
            Assert.IsInstanceOf<DocumentShortcut>(shortcut);
            Assert.AreEqual("document_name", ((DocumentShortcut)shortcut!).DocumentId);
        }
    }
}
