using Cachr.AspNetCore;
using Cachr.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCachr(builder.Configuration)
    .AddCachrAspNetCorePeering();


var app = builder.Build();

app.Run();
