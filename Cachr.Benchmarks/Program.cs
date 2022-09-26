// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cachr.Benchmarks;

var configuration = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithOption(ConfigOptions.JoinSummary, true)
    .WithOption(ConfigOptions.DisableLogFile, true);
try
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, configuration);
}
catch (InvalidOperationException e) when ((e.StackTrace?.Contains("Diagnosers.CompositeDiagnoser.DisplayResults") == true) &&
                                          e.Message == "Sequence contains no elements")
{
}
