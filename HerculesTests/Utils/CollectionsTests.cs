using NUnit.Framework;
using System;
using System.Collections.ObjectModel;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class CollectionsTests
    {
        [Test]
        public void SynchronizeSortedTest()
        {
            var empty = Array.Empty<int>();
            var array123 = new[] { 1, 2, 3 };

            var c1 = new ObservableCollection<int> { 1, 2, 3 };
            c1.SynchronizeSorted(empty);
            CollectionAssert.AreEqual(empty, c1);

            var c2 = new ObservableCollection<int>();
            c2.SynchronizeSorted(array123);
            CollectionAssert.AreEqual(array123, c2);

            var c3 = new ObservableCollection<int> { 2, 3, 4 };
            c3.SynchronizeSorted(array123);
            CollectionAssert.AreEqual(array123, c3);
        }
    }
}
