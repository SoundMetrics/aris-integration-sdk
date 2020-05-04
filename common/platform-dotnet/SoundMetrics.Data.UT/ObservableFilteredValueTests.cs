using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Data.FilterAdapters;
using SoundMetrics.Data.Filters;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace SoundMetrics.Data.Filter.Adapters.UT
{
    using Filter = UnweightedMedianFilter<int>;

    [TestClass]
    public class ObservableFilteredValueTests
    {
        [TestMethod]
        public void TestBuffering()
        {
            var inputs = new[] { 1, 2, 3, 4, 5, 6, 7, +100, -100 };
                /*
                    1
                    1 2
                    1 2 3
                    1 2 3 4
                    1 2 3 4 5
                    2 3 4 5 6
                    3 4 5 6 7
                    4 5 6 7 +100
                    -100 5 6 7 +100
                 */
            var expected = new[] { 3, 4, 5, 6, 6 };
            var actual = new List<int>();

            using (var observableFilteredValue = ObservableFilteredValue.Create(new Filter(5)))
            {
                var observable = (IObservable<int>)observableFilteredValue;
                using (var _ = observable.Subscribe(OnNextValue))
                {
                    QueueSamples();

                    void QueueSamples()
                    {
                        foreach (var input in inputs)
                        {
                            observableFilteredValue.AddValue(input);
                        }

                    }
                }

                void OnNextValue(int value)
                {
                    Console.WriteLine($"adding {value}");
                    actual.Add(value);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
