// ReSharper disable RedundantUsingDirective - Shared code, no guarantee a project has these important globally.
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
namespace Cachr.Core;

internal static class TaskExtensions
{
    public static async ValueTask IgnoreExceptions<T>(this ValueTask<T> valueTask,
        LogLevel logLevel = LogLevel.Warning,
        ILogger? logger = null)
    {
        try
        {
            await valueTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.Log(logLevel, ex, "Unhandled exception in ValueTask, ignoring.");
        }
    }

    public static async Task IgnoreExceptions<T>(this Task<T> task,
        LogLevel logLevel = LogLevel.Warning,
        ILogger? logger = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.Log(logLevel, ex, "Unhandled exception in Task, ignoring.");
        }
    }

    public static async ValueTask IgnoreExceptions(this ValueTask valueTask,
        LogLevel logLevel = LogLevel.Warning,
        ILogger? logger = null)
    {
        try
        {
            await valueTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.Log(logLevel, ex, "Unhandled exception in ValueTask, ignoring.");
        }
    }

    public static async Task IgnoreExceptions(this Task task,
        LogLevel logLevel = LogLevel.Warning,
        ILogger? logger = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.Log(logLevel, ex, "Unhandled exception in Task, ignoring.");
        }
    }
}
