using System;
using System.Collections;
using System.Collections.Generic;

namespace SoundMetrics.Aris.EnumerableHelpers
{
    public static class ArisEnumerables
    {
        public static IEnumerable<(T, T)> AsPairs<T>(this IEnumerable<T> ts)
        {
            return new Pairs<T>(ts);
        }

        private sealed class Pairs<T> : IEnumerable<(T, T)>
        {
            public Pairs(IEnumerable<T> ts)
            {
                this.ts = ts;
            }

            public IEnumerator<(T, T)> GetEnumerator()
            {
                return new PairsEnumerator<T>(ts);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new PairsEnumerator<T>(ts);
            }

            private readonly IEnumerable<T> ts;
        }

        private sealed class PairsEnumerator<T> : IEnumerator<(T, T)>
        {
            public PairsEnumerator(IEnumerable<T> ts)
            {
                this.ets = ts.GetEnumerator();
            }

            public (T, T) Current =>
                hasCurrentPair ? currentPair[0] : throw new Exception("No current value");

            object? IEnumerator.Current =>
                hasCurrentPair ? currentPair[0] : throw new Exception("No current value");

            public void Dispose() { }

            public bool MoveNext()
            {
                hasSavedValue = hasSavedValue || GetAValue(ets, out savedValue);
                if (hasSavedValue)
                {
                    if (GetAValue(ets, out T secondValue))
                    {
                        currentPair[0] = (savedValue, secondValue);
                        hasCurrentPair = true;

                        savedValue = secondValue;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                static bool GetAValue(IEnumerator<T> ets, out T value)
                {
                    if (ets.MoveNext())
                    {
                        value = ets.Current;
                        return true;
                    }
                    else
                    {
                        value = default;
                        return false;
                    }
                }
            }

            public void Reset() => throw new NotImplementedException();

            private readonly IEnumerator<T> ets;

            private bool hasSavedValue, hasCurrentPair;
            private T savedValue = default;
            private (T, T)[] currentPair = new (T, T)[1];
        }
    }
}
