using Cachr.Core;

SQLitePCL.Batteries.Init();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddObjectStorage(builder.Configuration.GetSection("Storage"));
var app = builder.Build();

app.MapGet("/", () => "Hello World!");



await app.RunAsync();
