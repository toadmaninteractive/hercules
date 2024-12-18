using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class CsvTests
    {
        [Test]
        public void DetectSeparatorTest()
        {
            ClassicAssert.AreEqual('\t', CsvUtils.GetSeparator("1\t2"));
            ClassicAssert.AreEqual('\t', CsvUtils.GetSeparator("\"1\"\t\"2\""));
            ClassicAssert.AreEqual(null, CsvUtils.GetSeparator(@"<?xml version=""1.0"" encoding=""windows-1251""?>"));
        }
    }
}
