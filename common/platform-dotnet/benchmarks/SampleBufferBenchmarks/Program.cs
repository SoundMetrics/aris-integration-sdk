// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using SampleBufferBenchmarks;

_ = BenchmarkRunner.Run<SpanVsPointersFill>();
_ = BenchmarkRunner.Run<SpanVsPointersAddOne>();
_ = BenchmarkRunner.Run<SpanVsPointersBoxFilter>();
