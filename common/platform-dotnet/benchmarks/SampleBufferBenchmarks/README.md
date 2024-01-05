# README

This project contains benchmarks that explore the performance difference on .NET
using raw buffers vs `Span<T>`. The algorithms are simple.

> **NOTE:** There can be some variance in the results, but in general the
pointer manipulation is faster than using `Span<T>`. In the case of sequential writes, it appears the JITter is smart enough to make the `Span<T>` code faster than the raw buffer code.

## SpanVsPointersFill

This is an overly simplistic look at filling a buffer. The benchmark in which `Span<T>.Fill()` is by far the fastest, as the runtime uses whatever method it deems best. And in this case the runtime always knows best.

The other two benchmarks implement the fill by setting one element at a time,
either through `Span<byte>` or maniuplating `byte` pointers directly.

```

BenchmarkDotNet v0.13.11, Windows 10 (10.0.19045.3803/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2


```
| Method                  | Mean       | Error    | StdDev   | Median     |
|------------------------ |-----------:|---------:|---------:|-----------:|
| SpanOneFillByElement    | 3,224.8 μs | 35.55 μs | 31.51 μs | 3,235.4 μs |
| SpanOneFill             |   368.4 μs |  8.09 μs | 23.46 μs |   359.9 μs |
| PointerOneFillByElement | 3,621.1 μs | 65.56 μs | 58.12 μs | 3,617.4 μs |

## SpanVsPointersAddOne

This implements an in-place `value = value + 1` transform.

```

BenchmarkDotNet v0.13.11, Windows 10 (10.0.19045.3803/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2


```
| Method        | Mean     | Error     | StdDev    |
|-------------- |---------:|----------:|----------:|
| SpanAddOne    | 5.766 ms | 0.0790 ms | 0.0739 ms |
| PointerAddOne | 3.723 ms | 0.0395 ms | 0.0369 ms |

## SpanVsPointersBoxFilter

This implements a naive **Box Filter** transform into an output buffer using `Span<T>` and pointer manipulation.

```

BenchmarkDotNet v0.13.11, Windows 10 (10.0.19045.3803/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2


```
| Method           | Mean     | Error     | StdDev    |
|----------------- |---------:|----------:|----------:|
| SpanBoxFilter    | 5.264 ms | 0.0798 ms | 0.0746 ms |
| PointerBoxFilter | 2.463 ms | 0.0466 ms | 0.0479 ms |

## Reproduction of Benchmark Results

The benchmarks are run from the project folder like this:

```cmd
dotnet run -c Release
```