using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cachr.AspNetCore;

public sealed class CachrWebSocketBusMiddleware
{
    private readonly RequestDelegate _next;

    public CachrWebSocketBusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path != "/$bus")
        {
            await _next.Invoke(context).ConfigureAwait(false);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        if (!context.Request.Query.TryGetValue("id", out var idStrings) || idStrings.Count != 1 ||
            !Guid.TryParse(idStrings.Single(), out var id))
        {
            context.Response.StatusCode = 400;
            return;
        }

        if (!context.Request.Query.TryGetValue("uri", out var uris) || uris.Count == 0)
        {
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
            context.Response.StatusCode = 400;
            return;
        }

        var uri = parsedUris.ToArray()[Random.Shared.Next(0, parsedUris.Count)];

        var webSocketAcceptContext = new WebSocketAcceptContext() {DangerousEnableCompression = true};
        var webSocket = await context.WebSockets.AcceptWebSocketAsync(webSocketAcceptContext).ConfigureAwait(false);
        var peerHandler = ActivatorUtilities.CreateInstance<CachrWebSocketPeer>(context.RequestServices, id, uri, webSocket);
        await peerHandler.RunPeerAsync(context.RequestAborted).ConfigureAwait(false);
    }
}
