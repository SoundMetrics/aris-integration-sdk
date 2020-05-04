// Copyright 2020 Sound Metrics Corp. All Rights Reserved.

using SoundMetrics.Data.Filters;
using System;
using System.ComponentModel;

namespace SoundMetrics.Data.FilterAdapters
{
    public static class FilteredProperty
    {
        public static IFilteredProperty<T> Create<T>(IBufferedFilter<T> filter)
        {
            return new FilteredPropertyImpl<T>(filter);
        }

        public interface IFilteredProperty<T> : INotifyPropertyChanged
        {
            void AddValue(T value);

            T FilteredValue { get; }
        }

        private sealed class FilteredPropertyImpl<T>
            : INotifyPropertyChanged, IFilteredProperty<T>
        {
            public FilteredPropertyImpl(IBufferedFilter<T> filter)
            {
                this.filter = filter;
            }

            public void AddValue(T value)
            {
                T newFilteredValue;

                if (filter.AddValue(value, out newFilteredValue))
                {
                    FilteredValue = newFilteredValue;
                }
            }

            public T FilteredValue
            {
                get => currentValue;

                private set
                {
                    if (!Object.Equals(currentValue, value))
                    {
                        currentValue = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(FilteredValueName));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private static readonly string FilteredValueName = nameof(FilteredValue);

            private readonly IBufferedFilter<T> filter;
            private T currentValue;
        }
    }
}
