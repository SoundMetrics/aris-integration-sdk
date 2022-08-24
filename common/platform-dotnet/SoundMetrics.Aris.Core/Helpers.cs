// Copyright (c) 2022 Sound Metrics Corp.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SoundMetrics.Aris.Core
{
    public static class Helpers
    {
        /// <summary>
        /// Ease-of-use function for generating static read-only dictionaries.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="keyValuePairs">Entries in the dictionary.</param>
        /// <returns>The populated read only dictionary.</returns>
        public static ReadOnlyDictionary<TKey, TValue>
            MakeReadOnlyDictionary<TKey, TValue>(
                params (TKey key, TValue value)[] keyValuePairs)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (var (key, value) in keyValuePairs)
            {
                dictionary.Add(key, value);
            }

            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }
    }
}
