using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoundMetrics.HID.Windows.UT
{
    using FlagReturnType = UInt32; // Avoid type mismatches in tests

    [TestClass]
    public class ButtonSelectionTest
    {
        [TestMethod]
        public void CorrectTestType()
        {
            // Because we're specifying specific types in the assertions.
            // This prevents invalid comparisons.

            var bs = new ButtonSelection();
            var result = bs.ToFlags();
            Type expected = typeof(FlagReturnType);

            Assert.IsNotNull(result);

            Type actual = result.GetType();
            Assert.AreEqual<Type>(expected, actual);
        }

        [TestMethod]
        public void NoButtons()
        {
            var bs = new ButtonSelection();
            var expected = 0u;
            Assert.AreEqual<FlagReturnType>(expected, bs.ToFlags());
        }

        [TestMethod]
        public void Button32()
        {
            var bs = new ButtonSelection() { EnableButton32 = true };
            var expected = Joystick.JOY_BUTTON32;
            Assert.AreEqual<FlagReturnType>(0b10000000000000000000000000000000, Joystick.JOY_BUTTON32);
            Assert.AreEqual<FlagReturnType>(expected, bs.ToFlags());
        }

        [TestMethod]
        public void EvenNumberedButtons()
        {
            // Note, the even-numbered buttons, because they're indexed
            // from 1, are the odd-numbered bits. :/

            var bs = new ButtonSelection()
            {
                EnableButton2 = true,
                EnableButton4 = true,
                EnableButton6 = true,
                EnableButton8 = true,
                EnableButton10 = true,
                EnableButton12 = true,
                EnableButton14 = true,
                EnableButton16 = true,
                EnableButton18 = true,
                EnableButton20 = true,
                EnableButton22 = true,
                EnableButton24 = true,
                EnableButton26 = true,
                EnableButton28 = true,
                EnableButton30 = true,
                EnableButton32 = true,
            };

            var expected = 0b10101010101010101010101010101010;
            Assert.AreEqual<FlagReturnType>(expected, bs.ToFlags());
        }

        [TestMethod]
        public void OddNumberedButtons()
        {
            // Note, the odd-numbered buttons, because they're indexed
            // from 1, are the even-numbered bits. :/

            var bs = new ButtonSelection()
            {
                EnableButton1 = true,
                EnableButton3 = true,
                EnableButton5 = true,
                EnableButton7 = true,
                EnableButton9 = true,
                EnableButton11 = true,
                EnableButton13 = true,
                EnableButton15 = true,
                EnableButton17 = true,
                EnableButton19 = true,
                EnableButton21 = true,
                EnableButton23 = true,
                EnableButton25 = true,
                EnableButton27 = true,
                EnableButton29 = true,
                EnableButton31 = true,
            };

            var expected = 0b01010101010101010101010101010101u;
            Assert.AreEqual<FlagReturnType>(expected, bs.ToFlags());
        }

        [TestMethod]
        public void AllButtons()
        {
            var bs = ButtonSelection.AllButtons;
            var expected = 0b11111111111111111111111111111111;
            Assert.AreEqual<FlagReturnType>(expected, bs.ToFlags());
        }
    }
}
