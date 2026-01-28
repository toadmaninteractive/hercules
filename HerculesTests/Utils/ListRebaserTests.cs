using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class ListRebaserTests
    {
        [Test]
        public void RebaseEmptyTest()
        {
            CollectionAssert.AreEqual(Array.Empty<int>(), ListRebaser.Rebase(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>()));
            CollectionAssert.AreEqual(Array.Empty<int>(), ListRebaser.Rebase(Array.Empty<int>(), [1, 2], [1, 2]));
        }

        [Test]
        public void RebaseTheirAddedTest()
        {
            CollectionAssert.AreEqual(new[] { 2 }, ListRebaser.Rebase(Array.Empty<int>(), [1], [1, 2]));
            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, ListRebaser.Rebase([1, 2], [1, 2], [1, 3, 2]));
            CollectionAssert.AreEqual(new[] { 3, 2 }, ListRebaser.Rebase([2], [1, 2], [1, 3, 2]));
        }

        [Test]
        public void RebaseComplexTest()
        {
            CollectionAssert.AreEqual(new[] { 5, 7, 6 }, ListRebaser.Rebase([2, 4, 6], [1, 2, 3, 4], [3, 5, 7]));
        }

        [Test]
        public void ReorderTest()
        {
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, ListRebaser.Rebase([3, 4, 5, 1, 2], [1, 2, 3, 4, 5], [5, 4, 3, 2, 1]));
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 1, 2 }, ListRebaser.Rebase([3, 4, 5, 1, 2], [1, 2, 3, 4, 5], [1, 2, 3, 4, 5]));
        }

        [Test]
        public void DoublesTest()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 3, 4, 2 }, ListRebaser.Rebase([1, 2, 3, 3, 4], [1, 2, 3, 4, 5], [1, 2, 3, 4, 2]));
        }
    }
}
