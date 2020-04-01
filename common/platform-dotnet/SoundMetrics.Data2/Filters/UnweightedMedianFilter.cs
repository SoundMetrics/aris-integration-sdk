// Copyright 2020 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.Data.Filters
{
    public class UnweightedMedianFilter<T>
        : IBufferedFilter<T>
        where T : IComparable, IComparable<T>
    {
        public UnweightedMedianFilter(int bufferSize)
        {
            if (bufferSize < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bufferSize),
                    "Choose a buffer of at least 3 items");
            }

            buffer = new T[bufferSize];
        }

        public bool AddValue(T value, out T filteredValue)
        {
            buffer[nextItemIndex] = value;
            nextItemIndex = (nextItemIndex + 1) % buffer.Length;

            itemCount = Math.Min(itemCount + 1, buffer.Length);
            var isBufferFull = itemCount == buffer.Length;

            Array.Sort(buffer, 0, itemCount);

            var currentValueIndex = itemCount / 2;
            var currentValue = buffer[currentValueIndex];

            filteredValue = currentValue;
            return isBufferFull;
        }

        private readonly T[] buffer;
        private int itemCount;
        private int nextItemIndex;
    }
}
