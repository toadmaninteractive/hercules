using Hercules.ApplicationUpdate;
using Hercules.Documents;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class HerculesUrlTests
    {
        [Test]
        public void TryGetDatabaseUrlTest()
        {
            var source = new Uri("hercules://host.io/database/document");
            ClassicAssert.IsTrue(HerculesUrl.TryGetDatabaseHerculesUrl(source, out var dbUrl));
            ClassicAssert.AreEqual(dbUrl!.ToString(), "hercules://host.io/database");
        }
    }
}
