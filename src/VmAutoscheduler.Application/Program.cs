using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using VmAutoscheduler.Application.Extensions;
using VmAutoscheduler.Application.Workers;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build())
    .CreateLogger();

var builder = Host.CreateDefaultBuilder(args);
try
{
    Log.Information("Starting host...");
    var host = builder
        .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddConfiguration())
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        })
        .ConfigureServices(Configure)
        .Build();
    host.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void Configure(HostBuilderContext context, IServiceCollection services)
{
    services
        .SetupSettings(context.Configuration)
        .AddServices()
        .AddHostedService<SchedulerWorker>();
}