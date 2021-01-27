using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SoundMetrics.Aris.Core.UT
{
    using Range = ValueRange<int>;

    [TestClass]
    public class RangeTests
    {
        private static Range R(int minimum, int maximum) => new Range(minimum, maximum);

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
    }
}
