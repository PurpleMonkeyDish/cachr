using Cachr.Core.Data;
using Cachr.Core.Data.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cachr.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageConfiguration>().Bind(configuration);
        services.AddDbContextPool<ObjectStorageContext>((provider, builder) =>
            builder.UseSqlite(provider.GetRequiredService<IOptions<StorageConfiguration>>().Value.ConnectionString));
        services.AddTransient<IStartupFilter, MigrationStartupFilter>();
        return services;
    }
}

public class MigrationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return context =>
        {
            using (var scope = context.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ObjectStorageContext>();

                dbContext.Database.Migrate();
            }
            next(context);
        };
    }
}
