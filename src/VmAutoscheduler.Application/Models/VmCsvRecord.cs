namespace VmAutoscheduler.Application.Models;

public class VmCsvRecord
{
    public string? TimestampUtc { get; set; } = string.Empty;

    public string? SubscriptionId { get; set; } = string.Empty;

    public string? ResourceGroup { get; set; } = string.Empty;

    public string? ComputerName { get; set; } = string.Empty;

    public string? PowerState { get; set; } = string.Empty;

    public bool Autoshutdown { get; set; }

    public string? VmStartTimeUtc { get; set; } = string.Empty;
}
