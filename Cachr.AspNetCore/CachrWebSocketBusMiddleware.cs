using System.Buffers;
using Cachr.Core.Buffers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cachr.AspNetCore;

public sealed class CachrWebSocketBusMiddleware
{
    private readonly RequestDelegate _next;

    public CachrWebSocketBusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CachrWebSocketBusMiddleware> logger)
    {
        if (context.Request.Path != "")
        {
            await _next.Invoke(context).ConfigureAwait(false);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            logger.LogWarning("Rejecting web socket request with 400, request isn't for a web socket, what?");
            context.Response.StatusCode = 400;
            return;
        }

        if (!context.Request.Query.TryGetValue("id", out var idStrings) || idStrings.Count != 1 ||
            !Guid.TryParse(idStrings.Single(), out var id))
        {
            logger.LogWarning("Rejecting web socket request with 400, missing or malformed id query argument.");
            context.Response.StatusCode = 400;
            return;
        }

        if (!context.Request.Query.TryGetValue("uri", out var uris) || uris.Count == 0)
        {
            logger.LogWarning("Rejecting web socket request with 400, missing uri query argument.");
            context.Response.StatusCode = 400;
            return;
        }

        var parsedUris = new HashSet<Uri>();

        foreach (var uriString in uris)
        {
            if (string.IsNullOrWhiteSpace(uriString))
            {
                continue;
            }

            if (!Uri.TryCreate(Uri.UnescapeDataString(uriString), UriKind.Absolute, out var result)) continue;
            parsedUris.Add(result);
        }

        if (parsedUris.Count == 0)
        {
            logger.LogWarning("Rejecting web socket request with 400, unable to parse URI arguments.");
            context.Response.StatusCode = 400;
            return;
        }
        using(logger.BeginScope("{id}", id))
        using (logger.BeginScope("{requestId}", context.Connection.Id))
        {
            logger.LogInformation("Accepting websocket");
            try
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                using var peerHandler =
                    ActivatorUtilities.CreateInstance<CachrWebSocketPeer>(context.RequestServices,
                        id,
                        parsedUris.ToArray(),
                        webSocket
                    );
                logger.LogInformation("Accepted websocket, starting receive loop.");
                await peerHandler.RunPeerAsync(context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Web socket peer processing loop failed");
            }
        }
    }
}
