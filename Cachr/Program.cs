using OrleansDashboard;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseDashboard(config =>
    {
        config.HostSelf = false;
        config.CounterUpdateIntervalMs = 10000;
    });
});

var app = builder.Build();

app.UseResponseCompression();
app.UseResponseCaching();
app.UseOrleansDashboard(options: new DashboardOptions()
{
    HostSelf = false,
    CounterUpdateIntervalMs = 10000,
    BasePath = "/dashboard"
});

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
