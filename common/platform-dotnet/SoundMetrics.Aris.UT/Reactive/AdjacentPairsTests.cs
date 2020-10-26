using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SoundMetrics.Aris.Reactive.UT
{
    [TestClass]
    public sealed class AdjacentPairsTests
    {
        private static void Pump<T>(ISubject<T> subject, T[] values)
        {
            foreach (var value in values)
            {
                subject.OnNext(value);
            }
        }

        private static List<(int,int)> TestPairsImpl(int[] inputs)
        {
            var actual = new List<(int, int)>();
            var subject = new Subject<int>();

            using (var subscription =
                Adjacent.ValuePairs(subject)
                    .Subscribe(pair =>
                    {
                        actual.Add(pair);
                    }))
            {
                Pump(subject, inputs);
                subject.OnCompleted();
            }

            return actual;
        }

        private static List<(string, string)> TestPairsImpl(string[] inputs)
        {
            var actual = new List<(string, string)>();
            var subject = new Subject<string>();

            using (var subscription =
                Adjacent.Pairs(subject)
                    .Subscribe(pair =>
                    {
                        actual.Add(pair);
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
            var expected = new (int, int)[0];
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmptyRefInputPairs()
        {
            var inputs = new string[0];
            var expected = new (string, string)[0];
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonValueInputPairs()
        {
            var inputs = new int[] { 1 };
            var expected = new (int, int)[0];
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SingletonRefInputPairs()
        {
            var inputs = new string[] { "a" };
            var expected = new (string, string)[0];
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleValueInputPairs()
        {
            var inputs = new int[] { 1, 2 };
            var expected = new (int, int)[] { (1, 2) };
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleRefInputPairs()
        {
            var inputs = new string[] { "a", "b" };
            var expected = new (string, string)[] { ("a", "b") };
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleValueInputPairs()
        {
            var inputs = new int[] { 1, 2, 3, 4 };
            var expected = new (int, int)[] { (1, 2), (2, 3), (3, 4) };
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleRefInputPairs()
        {
            var inputs = new string[] { "a", "b", "c", "d" };
            var expected = new (string, string)[] { ("a", "b"), ("b", "c"), ("c", "d") };
            var actual = TestPairsImpl(inputs);

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
