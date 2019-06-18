using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.HID.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.HID.Windows.UT
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(int, T)> MapI<T>(this IEnumerable<T> seq)
        {
            var i = 0;
            return seq.Select(t => (i++, t));
        }
    }

    [TestClass]
    public class JoystickButtonFilterTest
    {
        // NOTE
        // There is now IEnumerable<>.ForEach extension, so convert a range to a list
        // to get ForEach.
        // https://stackoverflow.com/questions/1509442/linq-style-for-each

        private static IEnumerable<T> Singleton<T>(T t) => new T[] { t };

        private static IEnumerable<uint> AllBitsTestSequence(uint initialState)
        {
            IEnumerable<uint> AllBits()
            {
                return Enumerable.Range(0, 31).ToList().Select(i => 1u << i);
            }

            // The first report is squelched, as we don't know the current state,
            // so use an initial state for the first elemnet.

            return Singleton(initialState)
                    .Concat(AllBits());
        }

        [TestMethod]
        public void TestAllBits()
        {
            var expected = 1u;

            foreach (var i in AllBitsTestSequence(initialState: 0u).Skip(1))
            {
                Assert.AreEqual<uint>(expected, i);
                expected = expected << 1;
            }
        }

        [TestMethod]
        public void NoButtonsOfInterest()
        {
            var bs = new ButtonSelection();
            var filter = JoystickObservable.CreateButtonFilter(bs);

            var eventsPassed = 0;
            var expectedPassed = 0;

            AllBitsTestSequence(initialState: ~0u)
                .ToList()
                .ForEach(i =>
                {
                    var report = new JoystickPositionReport();
                    report.JoystickInfo.dwButtons = i;
                    eventsPassed += filter(report) ? 1 : 0;
                });


            Assert.AreEqual(expectedPassed, eventsPassed);
        }

        [TestMethod]
        public void TestAllButtons()
        {
            var bs = ButtonSelection.AllButtons;
            var filter = JoystickObservable.CreateButtonFilter(bs);

            // Note: the first report is squelched, so duplicate it.
            AllBitsTestSequence(initialState: 0u)
                .MapI()
                .ToList()
                .ForEach(input =>
                {
                    var (index, i) = input;
                    var report = new JoystickPositionReport();
                    report.JoystickInfo.dwButtons = i;
                    var included = filter(report);

                    // Skip verifying the first, it's squelched by the filter.
                    if (index > 0)
                    {
                        var bin = Convert.ToString(i, toBase: 2);
                        Assert.IsTrue(included, $"Failed on 0b{bin} (bit {index-1})");
                    }
                });
        }
    }
}
