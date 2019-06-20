using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.HID.Windows;

namespace ButtonSelection_spec
{
    using FlagReturnType = UInt32; // Avoid type mismatches in tests

    [TestClass]
    public class InvalidButtonSelectionsAreInvalid
    {
        [TestMethod] public void ZeroOrLess()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ButtonSelection(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ButtonSelection(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ButtonSelection(int.MinValue));
        }

        [TestMethod] public void GreaterThanAllowed()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ButtonSelection(33));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ButtonSelection(int.MaxValue));
        }
    }

    [TestClass]
    public class ValidButtonSelectionHasABit
    {
        [TestMethod] public void EveryButtonHasABit()
        {
            for (int i = 1; i <= 32; ++i)
            {
                var bs = new ButtonSelection(i);
                var flags = bs.Flags;
                Assert.AreNotEqual(0u, flags, $"Failed on button {i}");
            }
        }

        private static readonly ValueTuple<int, uint>[] PositionToValueMap =
            new ValueTuple<int, uint>[]
            {
                (01, 0b1),
                (02, 0b10),
                (03, 0b100),
                (04, 0b1000),
                (05, 0b10000),
                (06, 0b100000),
                (07, 0b1000000),
                (08, 0b10000000),
                (09, 0b100000000),
                (10, 0b1000000000),
                (11, 0b10000000000),
                (12, 0b100000000000),
                (13, 0b1000000000000),
                (14, 0b10000000000000),
                (15, 0b100000000000000),
                (16, 0b1000000000000000),
                (17, 0b10000000000000000),
                (18, 0b100000000000000000),
                (19, 0b1000000000000000000),
                (20, 0b10000000000000000000),
                (21, 0b100000000000000000000),
                (22, 0b1000000000000000000000),
                (23, 0b10000000000000000000000),
                (24, 0b100000000000000000000000),
                (25, 0b1000000000000000000000000),
                (26, 0b10000000000000000000000000),
                (27, 0b100000000000000000000000000),
                (28, 0b1000000000000000000000000000),
                (29, 0b10000000000000000000000000000),
                (30, 0b100000000000000000000000000000),
                (31, 0b1000000000000000000000000000000),
                (32, 0b10000000000000000000000000000000),
            };

        private static uint LookUpPositionValue(int pos) => PositionToValueMap[pos - 1].Item2;

        [TestMethod] public void EveryButtonHasTheCorrectBit()
        {
            foreach (var (btn, expected) in PositionToValueMap)
            {
                var bs = new ButtonSelection(btn);
                Assert.AreEqual(expected, bs.Flags);
            }
        }

        [TestMethod] public void NeighboringPairs()
        {
            for (int i = 1; i <= 31; ++i)
            {
                var btn1 = i;
                var btn2 = i + 1;

                var expected = LookUpPositionValue(btn1) | LookUpPositionValue(btn2);
                var bs = new ButtonSelection(btn1, btn2);
                var actual = bs.Flags;
                var msg = $"pair({btn1},{btn2})";
                Assert.AreEqual(expected, actual, msg);
            }
        }
    }

    [TestClass]
    public class InitializeButtonSelectionFromJoyCaps
    {
        [TestMethod] public void InitializesOkay()
        {
            var tests = new[]
            {
                (0u, 0u),
                (1u, 0b1u),
                (2u, 0b11u),
                (3u, 0b111u),
                (32u, 0xFFFFFFFFu)
            };

            foreach (var (buttonCount, expected) in tests)
            {
                var caps = new JoyCaps { wNumButtons = buttonCount };
                var bs = ButtonSelection.FromJoyCaps(caps);
                var actual = bs.Flags;
                Assert.AreEqual(expected, actual);
            }

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    var caps = new JoyCaps { wNumButtons = 1000 };
                    var bs = ButtonSelection.FromJoyCaps(caps);
                });
        }
    }
}

