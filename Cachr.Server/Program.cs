using Cachr.Core;

var builder = WebApplication.CreateBuilder(args);
var configPath = Environment.GetEnvironmentVariable("USER_CONFIGURATION") ?? "./UserData/appsettings.json";
builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
builder.Services.AddObjectStorage(builder.Configuration.GetSection("Storage"));
builder.WebHost.UseDefaultServiceProvider(options =>
{
    if(builder.Environment.IsDevelopment())
        options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");


await app.RunAsync();
