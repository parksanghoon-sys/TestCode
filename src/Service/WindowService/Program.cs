using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using RedisSubscriberService.Interfaces;
using RedisSubscriberService.Services;
using System.Diagnostics;

Debugger.Launch();

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "RedisSubscribeService";
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        services.AddLogging(builder =>
        {
            builder.ClearProviders();

            var currentDIr = $"{Directory.GetCurrentDirectory()}";
            var path = "configs";
            var logConfigFile = "nlog.config";
            var logConfigPath = Path.Combine(currentDIr, path, logConfigFile);

            builder.AddNLog(logConfigPath);
        });
        services.AddTransient<IPubSub, RedisPubSub>();
        //services.AddSingleton<ISubscribeService, SubscribeService>();
        services.AddHostedService<SubscribeService>();
    }).Build();

////var loger = host.Services.GetService<ILogger<SubscribeService>>();

////var service = host.Services.GetService<ISubscribeService>();

////if(service is null)
////{
////    loger?.LogError("ISubscribeService is not set.");
////    throw new InvalidOperationException("Not Create Service");
////}

////await service.Process();
////loger?.LogInformation("Service Start");

////await host.WaitForShutdownAsync();
////loger?.LogInformation("Service Shutdown!");
var logger = host.Services.GetService<ILogger<SubscribeService>>();

await host.RunAsync();
logger?.LogInformation("Service Started!");


logger?.LogInformation("Service Shutdown!");

