using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cachr.UnitTests;

internal static class TestHostBuilder
{
    internal static WebApplicationFactory<Program> GetTestApplication()
    {
        var factory = new WebApplicationFactory<Program>();
        return factory.WithWebHostBuilder(BuildWebHost);
    }

    internal class IntegrationTestStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app => Configure(app, next);
        }

        private void Configure(IApplicationBuilder app, Action<IApplicationBuilder> next)
        {
            app.Use(async (context, func) =>
            {
                // The test host uses null here, and we don't want that.
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                await func.Invoke(context).ConfigureAwait(false);
            });
            next.Invoke(app);
        }
    }
    private static void BuildWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(s => s.Insert(0, ServiceDescriptor.Transient<IStartupFilter, IntegrationTestStartupFilter>()));
    }
}
