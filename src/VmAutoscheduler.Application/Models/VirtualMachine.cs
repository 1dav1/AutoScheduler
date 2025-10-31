using VmAutoscheduler.Application.Enums;

namespace VmAutoscheduler.Application.Models;

public class VirtualMachine
{
    public DateTime TimestampUtc { get; init; }

    public string? SubscriptionId { get; init; }

    public string? ResourceGroup { get; init; }

    public string? ComputerName { get; init; }

    public PowerState PowerState { get; init; }

    public bool AutoShutdown { get; init; }

    public DateTimeOffset? StartTimeUtc { get; init; }
}
