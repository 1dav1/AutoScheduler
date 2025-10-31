using Azure.Identity;
using Azure.Monitor.Query.Logs;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VmAutoscheduler.Application.Infrastructure;
using VmAutoscheduler.Application.Settings;

namespace VmAutoscheduler.Application.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection SetupSettings(this IServiceCollection services, IConfiguration configuration)
    {
        return services.Configure<SchedulerSettings>(configuration.GetSection(nameof(SchedulerSettings)));
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services
            .AddSingleton(provider =>
            {
                var credentials = new DefaultAzureCredential();
                return new ArmClient(credentials);
            })
            .AddSingleton(provider =>
            {
                var cred = new DefaultAzureCredential();
                return new LogsQueryClient(cred);
            })
            .AddTransient<ICsvWriter, CsvWriter>()
            .AddTransient<IStartTimeFetcher, StartTimeFetcher>()
            .AddTransient<IPowerRulesApplier, PowerRulesApplier>()
            .AddTransient<IVirtualMachineManager, VirtualMachineManager>();
        return services; 
    }
}
