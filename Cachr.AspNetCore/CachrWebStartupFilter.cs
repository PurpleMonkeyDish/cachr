using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

namespace Cachr.AspNetCore;

public sealed class CachrWebStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app => ConfigureApplicationAndContinue(app, next);
    }

    private static void ConfigureApplicationAndContinue(IApplicationBuilder app, Action<IApplicationBuilder> next)
    {
        app.Map("/$cachr", MapCachrUtilities);
        next.Invoke(app);
    }

    private static void MapCachrUtilities(IApplicationBuilder app)
    {
        app.UseResponseCompression();

        // We want the actual IP we can talk to the service on, if it went through a load balancer.
        app.UseForwardedHeaders(new ForwardedHeadersOptions() {ForwardedHeaders = ForwardedHeaders.All});
        app.UseWebSockets(new WebSocketOptions() {KeepAliveInterval = TimeSpan.FromSeconds(10)});

        app.Map(pathMatch: "/$whoami",
            static c =>
            {
                c.Run(async context => await context.Response.WriteAsJsonAsync(new
                {
                    ClientAddress = context.Connection.RemoteIpAddress,
                    ServerAddress = context.Connection.LocalIpAddress
                }));
            });
        app.Map(pathMatch: "/$bus", static c => c.UseMiddleware<CachrWebSocketBusMiddleware>());
    }
}
