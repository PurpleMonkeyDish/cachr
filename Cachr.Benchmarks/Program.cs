// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cachr.Benchmarks;

BenchmarkRunner.Run(typeof(Program).Assembly, // all benchmarks from given assembly are going to be executed
    ManualConfig
        .Create(DefaultConfig.Instance)
        .WithOption(ConfigOptions.JoinSummary, true)
        .WithOption(ConfigOptions.DisableLogFile, true)
    , args);
