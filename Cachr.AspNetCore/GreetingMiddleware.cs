using System.Text.Json;
using Cachr.Core;
using Microsoft.AspNetCore.Http;

namespace Cachr.AspNetCore;

public sealed class GreetingMiddleware
{
    private readonly RequestDelegate _next;

    public GreetingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method != "GET" || context.Request.Path != "")
        {
            await _next.Invoke(context).ConfigureAwait(false);
            return;
        }

        var response = new GreetingResponse(
            NodeIdentity.Id,
            NodeIdentity.Name,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.IsHttps
        );

        context.Response.StatusCode = 200;
        await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted)
            .ConfigureAwait(false);
    }
}
