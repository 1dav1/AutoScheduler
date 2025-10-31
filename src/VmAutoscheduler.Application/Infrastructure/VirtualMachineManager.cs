using Azure;
using Azure.Core;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VmAutoscheduler.Application.Constants;
using VmAutoscheduler.Application.Enums;
using VmAutoscheduler.Application.Models;
using VmAutoscheduler.Application.Settings;

namespace VmAutoscheduler.Application.Infrastructure;

public class VirtualMachineManager : IVirtualMachineManager
{
    private const char Delimiter = '/';

    private readonly IStartTimeFetcher _startTimeFetcher;
    private readonly ICsvWriter _csvWriter;
    private readonly IPowerRulesApplier _powerRulesApplier;
    private readonly SchedulerSettings _settings;
    private readonly ILogger<VirtualMachineManager> _logger;

    public VirtualMachineManager(
        IStartTimeFetcher startTimeFetcher,
        ICsvWriter csvWriter,
        IPowerRulesApplier powerRulesApplier,
        IOptionsMonitor<SchedulerSettings> monitor,
        ILogger<VirtualMachineManager> logger)
    {
        _startTimeFetcher = startTimeFetcher;
        _csvWriter = csvWriter;
        _powerRulesApplier = powerRulesApplier;
        _settings = monitor.CurrentValue;
        _logger = logger;
    }

    public async Task Manage(VirtualMachineResource virtualMachineResource, string subscriptionId, CancellationToken stoppingToken)
    {
        try
        {
            var virtualMachine = await GetVirtualMachine(virtualMachineResource, subscriptionId, stoppingToken);
            _csvWriter.Write(virtualMachine);
            if (virtualMachine.AutoShutdown)
            {
                await _powerRulesApplier.Apply(
                    virtualMachineResource,
                    virtualMachine.PowerState,
                    virtualMachine.StartTimeUtc,
                    stoppingToken);
            }
        }
        catch (Exception exVm)
        {
            _logger.LogError(exVm, "Error processing VM {Vm}", virtualMachineResource.Id);
        }
    }

    private async Task<VirtualMachine> GetVirtualMachine(
        VirtualMachineResource virtualMachineResource,
        string subscriptionId,
        CancellationToken stoppingToken)
    {
        string resourceGroup = ExtractResourceGroupName(virtualMachineResource.Id);
        string computerName = virtualMachineResource.Data.Name ?? string.Empty;
        bool autoShutdown = ExtractAutoShutdown(virtualMachineResource);

        Response<VirtualMachineInstanceView>? instanceViewResponse = null;
        try
        {
            instanceViewResponse = await virtualMachineResource.InstanceViewAsync(stoppingToken);
        }
        catch (RequestFailedException exception)
        {
            _logger.LogWarning(exception, "Could not get instance view for VM {VmId}", virtualMachineResource.Id);
        }

        PowerState powerState = ExtractPowerState(instanceViewResponse);
        var startTimeUtc = await _startTimeFetcher.GetAsync(
            _settings.WorkspaceId,
            virtualMachineResource.Data.Id,
            stoppingToken);

        return new VirtualMachine
        {
            TimestampUtc = DateTime.UtcNow,
            SubscriptionId = subscriptionId,
            ResourceGroup = resourceGroup,
            ComputerName = computerName,
            PowerState = powerState,
            AutoShutdown = autoShutdown,
            StartTimeUtc = startTimeUtc
        };
    }

    private static bool ExtractAutoShutdown(VirtualMachineResource virtualMachineResource)
    {
        bool autoShutdown = default;
        if (virtualMachineResource.Data.Tags != null &&
            virtualMachineResource.Data.Tags.TryGetValue(SchedulerConstants.AutoShutdown, out var tagValue))
        {
            autoShutdown = string.Equals(tagValue, SchedulerConstants.True);
        }

        return autoShutdown;
    }

    private static PowerState ExtractPowerState(Response<VirtualMachineInstanceView>? instanceViewResponse)
    {
        PowerState powerState = default;
        if (instanceViewResponse?.Value?.Statuses != null)
        {
            var statuses = instanceViewResponse.Value.Statuses;
            var powerStatus = statuses.FirstOrDefault(s =>
                s.Code != null &&
                s.Code.StartsWith(SchedulerConstants.Prefixes.PowerState, StringComparison.OrdinalIgnoreCase));
            if (powerStatus != null)
            {
                Enum.TryParse(
                    value: powerStatus.Code!,
                    ignoreCase: true,
                    result: out powerState);
            }
        }

        return powerState;
    }

    private static string ExtractResourceGroupName(ResourceIdentifier id)
    {
        var segments = id.ToString().Split(Delimiter, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals(
                SchedulerConstants.Prefixes.ResourceGroups,
                StringComparison.OrdinalIgnoreCase))
            {
                return segments[i + 1];
            }
        }
        return string.Empty;
    }
}
