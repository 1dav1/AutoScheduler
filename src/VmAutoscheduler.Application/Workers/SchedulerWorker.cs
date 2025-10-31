using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VmAutoscheduler.Application.Infrastructure;
using VmAutoscheduler.Application.Settings;

namespace VmAutoscheduler.Application.Workers;

public class SchedulerWorker : BackgroundService
{
    private readonly ArmClient _armClient;
    private readonly IVirtualMachineManager _virtualMachineManager;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SchedulerWorker> _logger;
    private readonly SchedulerSettings _options;
    private bool _shutdownRequested;

    public SchedulerWorker(
        ArmClient armClient,
        IVirtualMachineManager manager,
        IHostApplicationLifetime lifetime,
        ILogger<SchedulerWorker> logger,
        IOptionsMonitor<SchedulerSettings> monitor)
    {
        _armClient = armClient;
        _virtualMachineManager = manager;
        _lifetime = lifetime;
        _logger = logger;
        _options = monitor.CurrentValue;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStopping.Register(OnShutdown);
        return base.StartAsync(cancellationToken);
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Azure VM Watcher started. Poll interval: {IntervalSeconds}s",
            _options.PollIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested &&
            !_shutdownRequested)
        {
            try
            {
                await foreach (var subscription in
                    _armClient.GetSubscriptions().GetAllAsync(cancellationToken: stoppingToken))
                {
                    var subscriptionId = subscription.Data.SubscriptionId;
                    _logger.LogDebug("Processing subscription {SubscriptionId}", subscriptionId);
                    var virtualMachines = subscription.GetVirtualMachines(cancellationToken: stoppingToken);
                    var manageVirtualMachineTasks = virtualMachines
                        .Select(m => _virtualMachineManager.Manage(m, subscriptionId, stoppingToken));
                    await Task.WhenAll(manageVirtualMachineTasks);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while scanning subscriptions/VMs");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        _logger.LogInformation("Azure VM Watcher stopped.");
    }

    private void OnShutdown()
    {
        _shutdownRequested = true;
    }
}
