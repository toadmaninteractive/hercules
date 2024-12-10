using NUnit.Framework;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class CsvTests
    {
        [Test]
        public void DetectSeparatorTest()
        {
            Assert.AreEqual('\t', CsvUtils.GetSeparator("1\t2"));
            Assert.AreEqual('\t', CsvUtils.GetSeparator("\"1\"\t\"2\""));
            Assert.AreEqual(null, CsvUtils.GetSeparator(@"<?xml version=""1.0"" encoding=""windows-1251""?>"));
        }
    }
}
