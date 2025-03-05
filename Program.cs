using System.Runtime.InteropServices;
using FileWatcherService;
using FileWatcherService.services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ISendToApi, SendToApi>();
builder.Services.AddHttpClient();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    // .AddEnvironmentVariables()
    // .AddCommandLine(args)
    .Build();

var apiUrl = configuration["ApiSettings: ApiEndpoint"];
var apiKey = configuration["ApiSettings: ApiKey"];


var host = builder.Build();
host.Run();
