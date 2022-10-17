// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Cachr.Benchmarks;
public sealed class Program
{
    public static void Main(string[] args)
    {
        var configuration = ManualConfig
            .Create(DefaultConfig.Instance)
            .WithOption(ConfigOptions.JoinSummary, true)
            .WithOption(ConfigOptions.DisableLogFile, true);
        if (Debugger.IsAttached)
        {
            configuration.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
        try
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, configuration);
        }
        catch (InvalidOperationException e) when ((e.StackTrace?.Contains(
                                                       "Diagnosers.CompositeDiagnoser.DisplayResults") ==
                                                   true) &&
                                                  e.Message == "Sequence contains no elements")
        {
        }
    }
}
