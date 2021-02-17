using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SoundMetrics.Aris.Core.UT
{
    using Range = ValueRange<int>;

    [TestClass]
    public class RangeTests
    {
        private static Range R(int minimum, int maximum) => new Range(minimum, maximum);

        private static ValueRange<T> R<T>(T minimum, T maximum)
            where T : struct, IComparable<T>
            => new ValueRange<T>(minimum, maximum);

        [TestMethod]
        public void TestEqual()
        {
            var a = R(1, 2);
            var b = a;
            Assert.AreEqual(a, a);
            Assert.AreEqual(a, b);
            Assert.AreEqual(b, a);
        }

        [TestMethod]
        public void TestNotEqual()
        {
            var a = R(1, 2);
            var b = R(1, 3);
            var c = R(0, 2);

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, a);

            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(c, a);

            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(c, b);
        }

        [TestMethod]
        public void TestEmptyRange()
        {
            var a = R(1, 1);
            Assert.AreEqual(R(1, 1), a);
            Assert.IsTrue(a.IsEmpty);
        }

        [TestMethod]
        public void TestContains()
        {
            var a = R(1, 5);

            Assert.IsTrue(a.Contains(R(a.Minimum, a.Maximum)));

            Assert.IsFalse(a.Contains(R(a.Minimum - 1, a.Maximum)));
            Assert.IsFalse(a.Contains(R(a.Minimum, a.Maximum + 1)));

            Assert.IsTrue(a.Contains(R(1, 5)));
            Assert.IsTrue(a.Contains(R(2, 5)));
            Assert.IsTrue(a.Contains(R(3, 5)));
            Assert.IsTrue(a.Contains(R(4, 5)));
            Assert.IsTrue(a.Contains(R(5, 5)));

            Assert.IsTrue(a.Contains(R(1, 5)));
            Assert.IsTrue(a.Contains(R(1, 4)));
            Assert.IsTrue(a.Contains(R(1, 3)));
            Assert.IsTrue(a.Contains(R(1, 2)));
            Assert.IsTrue(a.Contains(R(1, 1)));
        }

        [TestMethod]
        public void TestConstrainTo()
        {
            var a = R(1, 5);
            var b = R(0, 5);
            var c = R(1, 6);
            var d = R(2, 2);

            Assert.AreEqual(a, a.ConstrainTo(b), $"{a}.ConstrainTo({b})");
            Assert.AreEqual(a, a.ConstrainTo(c), $"{a}.ConstrainTo({c}");

            Assert.AreEqual(d, a.ConstrainTo(d), $"{a}.ConstrainTo({d})");
            Assert.AreEqual(d, d.ConstrainTo(a), $"{d}.ConstrainTo({a})");
        }

        [TestMethod]
        public void IntersectEmptyLeft()
        {
            var (left, right) = (R(1, 1), R(1, 2));
            var actual = left.Intersect(right);
            Assert.IsTrue(actual.IsEmpty, $"{actual} is not empty");
        }

        [TestMethod]
        public void IntersectEmptyRight()
        {
            var (left, right) = (R(1, 2), R(1, 1));
            var actual = left.Intersect(right);
            Assert.IsTrue(actual.IsEmpty, $"{actual} is not empty");
        }

        [TestMethod]
        public void IntersectDisjointLesserLeft()
        {
            var (left, right) = (R(1, 2), R(2, 3));
            var actual = left.Intersect(right);
            Assert.IsTrue(actual.IsEmpty, $"{actual} is not empty");
        }

        [TestMethod]
        public void IntersectDisjointLesserRight()
        {
            var (left, right) = (R(3, 4), R(2, 3));
            var actual = left.Intersect(right);
            Assert.IsTrue(actual.IsEmpty, $"{actual} is not empty");
        }

        [TestMethod]
        public void IntersectOverlappingLesserLeft()
        {
            var (left, right) = (R(1, 3), R(2, 4));
            var expected = R(2, 3);
            var actual = left.Intersect(right);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IntersectOverlappingLesserRight()
        {
            var (left, right) = (R(3, 5), R(2, 4));
            var expected = R(3, 4);
            var actual = left.Intersect(right);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UnionEmptyLeft()
        {
            var (left, right) = (R(1, 1), R(2, 4));
            var expected = right;
            var actual = left.Union(right);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UnionEmptyRight()
        {
            var (left, right) = (R(1, 3), R(4, 4));
            var expected = left;
            var actual = left.Union(right);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UnionBothEmpty()
        {
            var (left, right) = (R(1, 1), R(4, 4));
            var actual = left.Union(right);
            Assert.IsTrue(actual.IsEmpty, $"{actual} is not empty");
        }

        [TestMethod]
        public void UnionIdentity()
        {
            var (left, right) = (R(1, 2), R(1, 2));
            var expected = R(1, 2);
            var actual = left.Union(right);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UnionOverlap()
        {
            var (left, right) = (R(1, 3), R(2, 4));
            var expected = R(1, 4);

            Assert.AreEqual(expected, left.Union(right));
            Assert.AreEqual(expected, right.Union(left));
        }

        [TestMethod]
        public void UnionContainedLower()
        {
            var (left, right) = (R(1, 3), R(1, 5));
            var expected = R(1, 5);

            Assert.AreEqual(expected, left.Union(right));
            Assert.AreEqual(expected, right.Union(left));
        }

        [TestMethod]
        public void UnionContainedUpper()
        {
            var (left, right) = (R(3, 5), R(1, 5));
            var expected = R(1, 5);

            Assert.AreEqual(expected, left.Union(right));
            Assert.AreEqual(expected, right.Union(left));
        }

        [TestMethod]
        public void UnionDisjoint()
        {
            var (left, right) = (R(1, 3), R(4, 5));

            var _ = Assert.ThrowsException<InvalidOperationException>(() => left.Union(right));
            _ = Assert.ThrowsException<InvalidOperationException>(() => right.Union(left));
        }

        [TestMethod]
        public void UnionAdjacentDistanceRanges()
        {
            // [1.000 m, 8.000 m] & [8.000 m, 20.000 m]
            var (left, right) =
                (R<Distance>(Distance.FromMeters(1), Distance.FromMeters(8)),
                R<Distance>(Distance.FromMeters(8), Distance.FromMeters(20)));
            var expected = R<Distance>(Distance.FromMeters(1), Distance.FromMeters(20));

            Assert.AreEqual(expected, left.Union(right));
            Assert.AreEqual(expected, right.Union(left));
        }
    }
}
