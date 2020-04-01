using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SoundMetrics.Data.Filters.UT
{
    using Filter = UnweightedMedianFilter<int>;

    [TestClass]
    public class UnweightedMedianFilterTests
    {
        [TestMethod]
        public void TestInvalidBufferSize()
        {
            // If we're filtering we should require at least two successive values
            // as inputs...
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var _ = new Filter(1);
            });
        }

        [TestMethod]
        public void TestBuffering()
        {
            var filter = new Filter(5);

            var cases = new[]
            {
                new { Input = 1, IsBufferFilled = false, ExpectedValue = 1 },
                new { Input = 2, IsBufferFilled = false, ExpectedValue = 2 },
                new { Input = 3, IsBufferFilled = false, ExpectedValue = 2 },
                new { Input = 4, IsBufferFilled = false, ExpectedValue = 3 },
                new { Input = 5, IsBufferFilled = true, ExpectedValue = 3 },
                new { Input = 6, IsBufferFilled = true, ExpectedValue = 4 },
                new { Input = 7, IsBufferFilled = true, ExpectedValue = 5 },
            };

            int caseIndex = 0;

            foreach (var c in cases)
            {
                var message = $"[{caseIndex}] Input={c.Input}; IsBufferFilled={c.IsBufferFilled}; ExpectedValue={c.ExpectedValue}";
                var expected = c.ExpectedValue;

                int actual;
                bool isBufferFilled = filter.AddValue(c.Input, out actual);

                Assert.AreEqual(c.IsBufferFilled, isBufferFilled, message);
                Assert.AreEqual(expected, actual, message);

                ++caseIndex;
            }
        }
    }
}
