// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Cachr.Benchmarks;

BenchmarkRunner.Run<DuplicateTrackerBenchmarks> (args: args);
BenchmarkRunner.Run<EndToEndBenchmarks>(args: args);
