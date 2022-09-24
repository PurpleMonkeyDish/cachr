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
        if (!context.WebSockets.IsWebSocketRequest || context.Request.Path != "/$bus")
        {
            await _next.Invoke(context).ConfigureAwait(false);
            return;
        }

        if (!context.Request.Query.TryGetValue("id", out var idStrings) || idStrings.Count != 1 ||
            !Guid.TryParse(idStrings.Single(), out var id))
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocketAcceptContext = new WebSocketAcceptContext() {DangerousEnableCompression = true};
        var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var peerHandler =
            ActivatorUtilities.CreateInstance<CachrWebSocketPeer>(context.RequestServices, webSocket, context, id);
        await using var _ = peerHandler.ConfigureAwait(false);

        await peerHandler.RunPeerAsync(context.RequestAborted).ConfigureAwait(false);
    }
}