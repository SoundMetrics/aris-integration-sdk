using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace SoundMetrics.Aris.Reactive
{
    [TestClass]
    public sealed class AdjacentTransformPairsTests
    {
        private static void Pump<T>(ISubject<T> subject, T[] values)
        {
            foreach (var value in values)
            {
                subject.OnNext(value);
            }
        }

        private static List<int> TestImpl(
            int[] inputs,
            Func<int, int, int> transform)
        {
            var actual = new List<int>();
            var subject = new Subject<int>();

            using (var subscription =
                Adjacent.TransformValuePairs(subject, transform)
                    .Subscribe(result =>
                    {
                        actual.Add(result);
                    }))
            {
                Pump(subject, inputs);
                subject.OnCompleted();
            }

            return actual;
        }

        private static List<string> TestImpl(
            string[] inputs,
            Func<string, string, string> transform)
        {
            var actual = new List<string>();
            var subject = new Subject<string>();

            using (var subscription =
                Adjacent.TransformPairs(subject, transform)
                    .Subscribe(result =>
                    {
                        actual.Add(result);
                    }))
            {
                Pump(subject, inputs);
                subject.OnCompleted();
            }

            return actual;
        }

        [TestMethod]
        public void EmptyValueInputPairs()
        {
            var inputs = new int[0];
            var expected = new int[0];
            Func<int, int, int> transform = (a, b) => a * b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmptyRefInputPairs()
        {
            var inputs = new string[0];
            var expected = new string[0];
            Func<string, string, string> transform = (a, b) => a + b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonValueInputPairs()
        {
            var inputs = new int[] { 1 };
            var expected = new int[0];
            Func<int, int, int> transform = (a, b) => a * b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonRefInputPairs()
        {
            var inputs = new string[] { "a" };
            var expected = new string[0];
            Func<string, string, string> transform = (a, b) => a + b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleValueInputPairs()
        {
            var inputs = new int[] { 3, 4 };
            var expected = new int[] { 12 };
            Func<int, int, int> transform = (a, b) => a * b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleRefInputPairs()
        {
            var inputs = new string[] { "a", "b" };
            var expected = new string[] { "ab" };
            Func<string, string, string> transform = (a, b) => a + b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleValueInputPairs()
        {
            var inputs = new int[] { 3, 4, 5, 6 };
            var expected = new int[] { 12, 20, 30 };
            Func<int, int, int> transform = (a, b) => a * b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleRefInputPairs()
        {
            var inputs = new string[] { "a", "b", "c", "d" };
            var expected = new string[] { "ab", "bc", "cd" };
            Func<string, string, string> transform = (a, b) => a + b;

            var actual = TestImpl(inputs, transform);

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
