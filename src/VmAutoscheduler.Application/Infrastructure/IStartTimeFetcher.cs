namespace VmAutoscheduler.Application.Infrastructure;

public interface IStartTimeFetcher
{
    Task<DateTimeOffset?> GetAsync(
        string workspaceId,
        string virtualMachineId,
        CancellationToken stoppingToken);
}