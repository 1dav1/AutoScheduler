namespace VmAutoscheduler.Application.Settings;

public class SchedulerSettings
{
    public string CsvOutputPath { get; set; } = "vm_status.csv";

    public int PollIntervalSeconds { get; set; } = 300;

    public bool EnableUptimeCheck { get; set; } = false;

    public string WorkspaceId { get; set; } = string.Empty;
}
