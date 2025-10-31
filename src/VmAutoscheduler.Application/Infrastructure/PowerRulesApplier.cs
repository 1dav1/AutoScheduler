using Azure;
using Azure.ResourceManager.Compute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VmAutoscheduler.Application.Enums;
using VmAutoscheduler.Application.Settings;

namespace VmAutoscheduler.Application.Infrastructure;

public class PowerRulesApplier : IPowerRulesApplier
{
    private readonly SchedulerSettings _settings;
    private readonly ILogger<PowerRulesApplier> _logger;

    public PowerRulesApplier(IOptionsMonitor<SchedulerSettings> monitor, ILogger<PowerRulesApplier> logger)
    {
        _settings = monitor.CurrentValue;
        _logger = logger;
    }

    public async Task Apply(
        VirtualMachineResource virtualMachine,
        PowerState powerState,
        DateTimeOffset? startTimeUtc,
        CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogDebug("Evaluating power rules for VM {Vm}", virtualMachine.Id);
            await HandlePowerState(powerState, virtualMachine, startTimeUtc, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying power rules to VM {Vm}", virtualMachine.Id);
        }
    }

    private async Task HandlePowerState(
        PowerState powerState,
        VirtualMachineResource virtualMachine,
        DateTimeOffset? startTimeUtc,
        CancellationToken stoppingToken)
    {
        if (powerState == PowerState.Running)
        {
            await HandleRunning(virtualMachine, startTimeUtc, stoppingToken);
        }
        else if (powerState == PowerState.Stopped)
        {
            await HandleStopped(virtualMachine, stoppingToken);
        }
        else
        {
            _logger.LogDebug(
                "VM {Vm} power state {State} not subject to autoshutdown rules.",
                virtualMachine.Id,
                powerState);
        }
    }

    private async Task HandleRunning(VirtualMachineResource virtualMachine, DateTimeOffset? startTimeUtc, CancellationToken stoppingToken)
    {
        bool runningDurationChecked = default;
        TimeSpan runningDuration = TimeSpan.Zero;
        if (_settings.EnableUptimeCheck)
        {
            runningDurationChecked = CheckRunningDuration(virtualMachine, startTimeUtc, ref runningDuration);
        }

        if (runningDurationChecked)
        {
            await ApplyPowerOff(virtualMachine, runningDuration, stoppingToken);
        }
        else
        {
            _logger.LogDebug(
                "Cannot determine runtime for VM {Vm}. Uptime-based shutdown skipped (EnableUptimeCheck={Enable}).",
                virtualMachine.Id,
                _settings.EnableUptimeCheck);
        }
    }

    private bool CheckRunningDuration(VirtualMachineResource virtualMachine, DateTimeOffset? startTimeUtc, ref TimeSpan runningDuration)
    {
        try
        {
            if (startTimeUtc.HasValue)
            {
                runningDuration = DateTimeOffset.UtcNow - startTimeUtc.Value;
                return true;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Uptime check failed for VM {Vm}. Skipping uptime-based shutdown.",
                virtualMachine.Id);
        }

        return false;
    }

    private async Task ApplyPowerOff(VirtualMachineResource virtualMachine, TimeSpan runningDuration, CancellationToken stoppingToken)
    {
        if (runningDuration > TimeSpan.FromHours(8))
        {
            _logger.LogInformation(
                "VM {Vm} has been running for {Duration}. Attempting to power off (Autoshutdown).",
                virtualMachine.Id,
                runningDuration);
            try
            {
                await virtualMachine.PowerOffAsync(WaitUntil.Completed, cancellationToken: stoppingToken);
                _logger.LogInformation("PowerOff initiated for VM {Vm}", virtualMachine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to power off VM {Vm}", virtualMachine.Id);
            }
        }
        else
        {
            _logger.LogDebug(
                "VM {Vm} running duration {Duration} is less than threshold.",
                virtualMachine.Id,
                runningDuration);
        }
    }

    private async Task HandleStopped(VirtualMachineResource virtualMachine, CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "VM {Vm} is stopped but allocated. Attempting to deallocate (Autoshutdown).",
            virtualMachine.Id);
        try
        {
            await virtualMachine.DeallocateAsync(WaitUntil.Completed, cancellationToken: stoppingToken);
            _logger.LogInformation("Deallocate initiated for VM {Vm}", virtualMachine.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deallocate VM {Vm}", virtualMachine.Id);
        }
    }
}
