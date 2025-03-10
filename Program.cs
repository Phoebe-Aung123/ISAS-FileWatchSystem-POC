using System.Runtime.InteropServices;
using ChoETL;
using FileWatcherService;
using FileWatcherService.services;

var builder = Host.CreateApplicationBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    // .AddEnvironmentVariables()
    // .AddCommandLine(args)
    .Build();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IConfiguration>(configuration);
builder.Services.AddSingleton<ISendToApi, SendToApi>();
builder.Services.AddHttpClient();


var host = builder.Build();
host.Run();
