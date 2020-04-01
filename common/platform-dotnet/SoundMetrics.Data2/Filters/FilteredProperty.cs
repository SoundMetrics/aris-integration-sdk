﻿// Copyright 2020 Sound Metrics Corp. All Rights Reserved.

using System;
using System.ComponentModel;

namespace SoundMetrics.Data.Filters
{
    public static class FilteredProperty
    {
        public static IFilteredProperty<T> Create<T>(IBufferedFilter<T> filter)
        {
            return new FilteredPropertyImpl<T>(filter);
        }

        public interface IFilteredProperty<T> : INotifyPropertyChanged
        {
            T FilteredValue { get; }
        }

        private class FilteredPropertyImpl<T>
            : INotifyPropertyChanged, IFilteredProperty<T>
        {
            public FilteredPropertyImpl(IBufferedFilter<T> filter)
            {
                this.filter = filter;
            }

            public void AddValue(T value)
            {
                T newValue;

                if (filter.AddValue(value, out newValue))
                {
                    FilteredValue = newValue;
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
