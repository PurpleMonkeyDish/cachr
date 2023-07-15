using Cachr.Core.Cache;
using Cachr.Core.Data;
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
        services.AddOptions<StorageObjectConfiguration>().Bind(configuration);
        services.AddOptions<ReaperConfiguration>().Bind(configuration);
        services.AddDbContextPool<ObjectStorageContext>((provider, builder) =>
            builder.UseSqlite(
                provider.GetRequiredService<IOptions<StorageObjectConfiguration>>().Value.ConnectionString));
        services.AddTransient<IStartupFilter, MigrationStartupFilter>();
        services.AddTransient<ICacheStorage, CacheStorage>();
        services.AddTransient<IDataMapper, DataMapper>();
        services.AddSingleton<ICacheFileManager, CacheFileManager>();
        services.AddSingleton<IShardSelector, ShardSelector>();
        services.AddHostedService<GrimReaper>();
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
