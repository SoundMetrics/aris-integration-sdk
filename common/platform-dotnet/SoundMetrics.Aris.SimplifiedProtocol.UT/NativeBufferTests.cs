using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoundMetrics.Aris.SimplifiedProtocol.UT
{
    [TestClass]
    public class NativeBufferTests
    {
        private static void AssertArraysAreEqual<T>(T[] a, T[] b)
            where T : IEquatable<T>
        {
            if (a.Length != b.Length)
            {
                Assert.Fail("Arrays are different lengths");
            }

            for (int i = 0; i < a.Length; ++i)
            {
                if (!a[i].Equals(b[i]))
                {
                    Assert.Fail($"Element at index {i} differs");
                }
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void HasExpectedValueAfterInitialization()
        {
            var input = new byte[] { 1, 2, 3, 4, 5 };
            var expected = input;

            using (var buffer = new NativeBuffer(input))
            {
                var actual = buffer.ToManagedArray();
                Assert.IsNotNull(actual);
                AssertArraysAreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void DoNotAllowEmptyArrayInit()
        {
            Assert.ThrowsException<ArgumentException>(
                () =>
                {
                    using (var buffer = new NativeBuffer(new byte[0]))
                    {
                    }
                });
        }

        [TestMethod]
        public void NullInputArray()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    using (var buffer = new NativeBuffer((byte[])null))
                    {
                    }
                });
        }

        [TestMethod]
        public void NullInputSegment()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    using (var buffer = new NativeBuffer((ArraySegment<byte>)null))
                    {
                    }
                });
        }

        [TestMethod]
        public void NullInputSegments()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    using (var buffer =
                        new NativeBuffer((IEnumerable<ArraySegment<byte>>)null))
                    {
                    }
                });
        }

        [TestMethod]
        public void SingleArraySegment()
        {
            var values = new byte[] { 1, 2, 3, 4, 5 };
            var input = new ArraySegment<byte>(values, 1, 3);
            var expected = new byte[] { 2, 3, 4 };

            using (var buffer = new NativeBuffer(input))
            {
                var actual = buffer.ToManagedArray();
                Assert.IsNotNull(actual);
                AssertArraysAreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void MultipleArraySegment()
        {
            var values = new byte[] { 1, 2, 3, 4, 5 };
            var inputs = new[]
            {
                new ArraySegment<byte>(values, 0, 3),
                new ArraySegment<byte>(values, 1, 3),
                new ArraySegment<byte>(values, 2, 3),
            };
            var expected = new byte[]
            {
                1, 2, 3,
                2, 3, 4,
                3, 4, 5,
            };

            using (var buffer = new NativeBuffer(inputs))
            {
                var actual = buffer.ToManagedArray();
                Assert.IsNotNull(actual);
                AssertArraysAreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void EmptyArraySegmentarray()
        {
            var inputs = new ArraySegment<byte>[0];

            Assert.ThrowsException<ArgumentException>(() =>
            {
                using (var buffer = new NativeBuffer(inputs))
                {
                }
            });
        }
    }
}
