using System;
using System.Reactive.Subjects;

namespace SoundMetrics.Aris.Reactive
{
    public static class Adjacent
    {
        public static IObservable<(T, T)> ValuePairs<T>(this IObservable<T> ts)
            where T : struct
        {
            var subject = new Subject<(T, T)>();
            IDisposable? subscription = null;
            T? previousValue = default;

            subscription = ts.Subscribe(
                onNext: t =>
                {
                    if (!(previousValue is null))
                    {
                        var pair = (previousValue.Value, t);
                        subject.OnNext(pair);
                    }

                    previousValue = t;
                },
                onCompleted: () =>
                {
                    subscription?.Dispose();
                    subject.OnCompleted();
                    subject.Dispose();
                });

            return subject;
        }

        public static IObservable<(T, T)> Pairs<T>(this IObservable<T> ts)
            where T : class
        {
            var subject = new Subject<(T, T)>();
            IDisposable? subscription = null;
            T? previousValue = default;

            subscription = ts.Subscribe(
                onNext: t =>
                {
                    if (!(previousValue is null))
                    {
                        var pair = (previousValue, t);
                        subject.OnNext(pair);
                    }

                    previousValue = t;
                },
                onCompleted: () =>
                {
                    subscription?.Dispose();
                    subject.OnCompleted();
                    subject.Dispose();
                });

            return subject;
        }

#pragma warning disable CA1715 // Identifiers should have correct prefix
        public static IObservable<U> TransformValuePairs<T, U>(this IObservable<T> ts, Func<T, T, U> transform)
#pragma warning restore CA1715 // Identifiers should have correct prefix
            where T : struct
        {
            var subject = new Subject<U>();
            IDisposable? subscription = null;
            var pairs = ValuePairs(ts);

            subscription = pairs.Subscribe(
                onNext: pair =>
                {
                    var (t1, t2) = pair;
                    var transformed = transform(t1, t2);
                    subject.OnNext(transformed);
                },
                onCompleted: () =>
                {
                    subscription?.Dispose();
                    subject.OnCompleted();
                    subject.Dispose();
                });

            return subject;
        }

#pragma warning disable CA1715 // Identifiers should have correct prefix
        public static IObservable<U> TransformPairs<T, U>(this IObservable<T> ts, Func<T, T, U> transform)
#pragma warning restore CA1715 // Identifiers should have correct prefix
            where T : class
        {
            var subject = new Subject<U>();
            IDisposable? subscription = null;
            var pairs = Pairs(ts);

            subscription = pairs.Subscribe(
                onNext: pair =>
                {
                    var (t1, t2) = pair;
                    var transformed = transform(t1, t2);
                    subject.OnNext(transformed);
                },
                onCompleted: () =>
                {
                    subscription?.Dispose();
                    subject.OnCompleted();
                    subject.Dispose();
                });

            return subject;
        }
    }
}
