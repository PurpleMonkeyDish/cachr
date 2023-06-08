// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Cachr.Benchmarks;
using Microsoft.CodeAnalysis;

if (!args.Contains("--filter"))
{
    args = args.Append("--filter").Append("*").ToArray();
}



BenchmarkSwitcher.FromAssembly(typeof(BitEncoderBenchmarks).Assembly).Run(args);
