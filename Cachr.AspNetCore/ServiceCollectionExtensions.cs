using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cachr.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCachrAspNetCorePeering(this IServiceCollection services)
    {
        // We must be configured at the expected path.
        // So we use a startup filter to configure ourselves, before anything else (Hopefully)
        services.AddTransient<IStartupFilter, CachrWebStartupFilter>();
        return services;
    }
}
