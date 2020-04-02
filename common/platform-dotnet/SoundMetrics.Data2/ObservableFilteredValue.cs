// Copyright 2020 Sound Metrics Corp. All Rights Reserved.

using SoundMetrics.Data.Filters;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace SoundMetrics.Data2
{
    public static class ObservableFilteredValue
    {
        public static IObservable<T> Create<T>(IBufferedFilter<T> filter)
        {
            return new ObservableFilteredValueImpl<T>(filter);
        }

        public interface IObservableFilteredValue<T> : IObservable<T>, IDisposable
        {
            void AddValue(T value);
        }

        private sealed class ObservableFilteredValueImpl<T> : IObservableFilteredValue<T>
        {
            public ObservableFilteredValueImpl(IBufferedFilter<T> filter)
            {
                this.filter = filter;
            }

            public void AddValue(T value)
            {
                T newFilteredValue;

                if (filter.AddValue(value, out newFilteredValue) && subject.HasObservers)
                {
                    subject.OnNext(newFilteredValue);
                }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return subject.Subscribe(observer);
            }

            public void Dispose()
            {
                subject.OnCompleted();
                subject.Dispose();
            }

            private readonly IBufferedFilter<T> filter;
            private readonly Subject<T> subject = new Subject<T>();
        }
    }
}
