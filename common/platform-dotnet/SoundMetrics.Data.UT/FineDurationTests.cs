using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using SoundMetrics.Data;

namespace SoundMetrics.Data.UT
{
    /// <summary>
    /// Testing this F# value type in C# to ensure easy interop.
    /// </summary>
    [TestClass]
    public class FineDurationTests
    {
        [TestMethod]
        public void TestEquality()
        {
            var a = FineDuration.FromMicroseconds(1);
            var b = FineDuration.FromMicroseconds(1);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a, FineDuration.OneMicrosecond);
            Assert.AreNotEqual(FineDuration.OneMicrosecond, FineDuration.Zero);

            var eq = (IEquatable<FineDuration>)a;
            Assert.IsTrue(eq.Equals(a));
            Assert.IsFalse(eq.Equals(a + a));
        }

        [TestMethod]
        public void TestComparable()
        {
            Assert.IsTrue(FineDuration.Zero.CompareTo(FineDuration.OneMicrosecond) < 0);
            Assert.IsTrue(FineDuration.OneMicrosecond.CompareTo(FineDuration.Zero) > 0);
            Assert.IsTrue(FineDuration.OneMicrosecond.CompareTo(FineDuration.OneMicrosecond) == 0);
        }

        [TestMethod]
        public void TestComparisonOperators()
        {
            // Makes use of LT, GT, EQ, NEQ, GTEQ, LTEQ, operators, some of which have to be
            // specially defined in F# to be used in other CLI languages.

            Assert.IsTrue(FineDuration.Zero == FineDuration.Zero);
            Assert.IsTrue(FineDuration.Zero != FineDuration.OneMicrosecond);

            Assert.IsTrue(FineDuration.Zero < FineDuration.OneMicrosecond);
            Assert.IsTrue(FineDuration.Zero <= FineDuration.Zero);
            Assert.IsTrue(FineDuration.Zero <= FineDuration.FromMicroseconds(double.Epsilon));

            Assert.IsTrue(FineDuration.OneMicrosecond > FineDuration.Zero);
            Assert.IsTrue(FineDuration.Zero >= FineDuration.Zero);
            Assert.IsTrue(FineDuration.FromMicroseconds(double.Epsilon) >= FineDuration.Zero);
        }

        [TestMethod]
        public void TestAdd()
        {
            var expected = FineDuration.FromMicroseconds(4);
            var a = FineDuration.FromMicroseconds(1);
            var b = FineDuration.FromMicroseconds(3);
            var actual = a + b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(0);
            a = FineDuration.FromMicroseconds(+1);
            b = FineDuration.FromMicroseconds(-1);
            actual = a + b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(0);
            a = FineDuration.FromMicroseconds(-1);
            b = FineDuration.FromMicroseconds(+1);
            actual = a + b;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSubtract()
        {
            FineDuration expected, a, b, actual;

            expected = FineDuration.FromMicroseconds(1);
            a = FineDuration.FromMicroseconds(1);
            b = FineDuration.Zero;
            actual = a - b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(0);
            a = FineDuration.FromMicroseconds(1);
            b = FineDuration.OneMicrosecond;
            actual = a - b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(-1);
            a = FineDuration.FromMicroseconds(0);
            b = FineDuration.OneMicrosecond;
            actual = a - b;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMultiply()
        {
            FineDuration expected, a, actual;
            double b;

            expected = FineDuration.FromMicroseconds(0);
            a = FineDuration.FromMicroseconds(0);
            b = 4;
            actual = a * b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(2);
            a = FineDuration.FromMicroseconds(2);
            b = 1;
            actual = a * b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(10);
            a = FineDuration.FromMicroseconds(2);
            b = 5;
            actual = a * b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(-5);
            a = FineDuration.FromMicroseconds(-1);
            b = 5;
            actual = a * b;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestDivide()
        {
            FineDuration expected, a, actual;
            double b;

            expected = FineDuration.FromMicroseconds(double.PositiveInfinity);
            a = FineDuration.FromMicroseconds(1);
            b = 0;
            actual = a / b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(8);
            a = FineDuration.FromMicroseconds(8);
            b = 1;
            actual = a / b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(4);
            a = FineDuration.FromMicroseconds(8);
            b = 2;
            actual = a / b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(16);
            a = FineDuration.FromMicroseconds(8);
            b = 0.5;
            actual = a / b;
            Assert.AreEqual(expected, actual);

            expected = FineDuration.FromMicroseconds(-4);
            a = FineDuration.FromMicroseconds(-8);
            b = 2;
            actual = a / b;
            Assert.AreEqual(expected, actual);
        }
    }
}
