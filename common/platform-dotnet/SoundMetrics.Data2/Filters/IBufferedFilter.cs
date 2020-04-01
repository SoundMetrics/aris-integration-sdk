// Copyright 2020 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Data.Filters
{
    public interface IBufferedFilter<T>
    {
        bool AddValue(T value, out T filteredValue);
    }
}
