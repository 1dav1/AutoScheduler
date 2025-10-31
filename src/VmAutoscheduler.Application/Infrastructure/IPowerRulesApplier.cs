using Azure.ResourceManager.Compute;
using VmAutoscheduler.Application.Enums;

namespace VmAutoscheduler.Application.Infrastructure;

public interface IPowerRulesApplier
{
    Task Apply(
        VirtualMachineResource virtualMachine,
        PowerState powerState,
        DateTimeOffset? startTime,
        CancellationToken stoppingToken);
}