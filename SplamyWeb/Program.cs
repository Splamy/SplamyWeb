using Microsoft.Extensions.Logging;
using NLog.Web;
using Microsoft.AspNetCore.Builder;
using SplamyWeb;

var app = WebApplication.CreateBuilder(args);

app.Logging.ClearProviders();
app.Logging.SetMinimumLevel(LogLevel.Trace);
app.Logging.AddNLogWeb();

Startup.ConfigureServices(app.Configuration, app.Services);

var webHost = app.Build();

await Startup.Configure(webHost);

// Create a new scope

await webHost.RunAsync();
