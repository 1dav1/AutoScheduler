using Azure.Monitor.Query.Logs;
using Microsoft.Extensions.Logging;
using VmAutoscheduler.Application.Constants;

namespace VmAutoscheduler.Application.Infrastructure;

public class StartTimeFetcher : IStartTimeFetcher
{
    private readonly LogsQueryClient _logsQueryClient;
    private readonly ILogger<StartTimeFetcher> _logger;

    public StartTimeFetcher(LogsQueryClient logsQueryClient, ILogger<StartTimeFetcher> logger)
    {
        _logsQueryClient = logsQueryClient;
        _logger = logger;
    }

    public async Task<DateTimeOffset?> GetAsync(
        string workspaceId,
        string virtualMachineId,
        CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return null;
        }

        var query = $@"
            AzureActivity
            | where ResourceId == '{virtualMachineId}'
            | where OperationNameValue == 'Microsoft.Compute/virtualMachines/start/action'
            | order by TimeGenerated desc
            | take 1
            | project TimeGenerated";
        try
        {
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                workspaceId,
                query,
                TimeSpan.FromDays(30),
                cancellationToken: stoppingToken
            );

            var table = response.Value.Table;
            if (table.Rows.Count == 0)
            {
                return null;
            }

            return table.Rows[0].GetDateTimeOffset(SchedulerConstants.TimeGenerated);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to query Log Analytics for VM start time (vm={Vm}).",
                virtualMachineId);
            return null;
        }
    }
}
