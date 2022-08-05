using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.EnumerableHelpers;
using System.Linq;

namespace SoundMetrics.Aris.Enumerable
{
    [TestClass]
    public sealed class EnumerablePairsTests
    {
        [TestMethod]
        public void EmptyValueInputPairs()
        {
            var inputs = new int[0];
            var expected = new (int, int)[0];
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmptyRefInputPairs()
        {
            var inputs = new string[0];
            var expected = new (string, string)[0];
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonValueInputPairs()
        {
            var inputs = new int[] { 1 };
            var expected = new (int, int)[0];
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonRefInputPairs()
        {
            var inputs = new string[] { "a" };
            var expected = new (string, string)[0];
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleValueInputPairs()
        {
            var inputs = new int[] { 1, 2 };
            var expected = new (int, int)[] { (1, 2) };
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleRefInputPairs()
        {
            var inputs = new string[] { "a", "b" };
            var expected = new (string, string)[] { ("a", "b") };
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleValueInputPairs()
        {
            var inputs = new int[] { 1, 2, 3, 4 };
            var expected = new (int, int)[] { (1, 2), (2, 3), (3, 4) };
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleRefInputPairs()
        {
            var inputs = new string[] { "a", "b", "c", "d" };
            var expected = new (string, string)[] { ("a", "b"), ("b", "c"), ("c", "d") };
            var actual = inputs.AsPairs().ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
