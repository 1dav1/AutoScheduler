using Azure.ResourceManager.Compute;

namespace VmAutoscheduler.Application.Infrastructure
{
    public interface IVirtualMachineManager
    {
        Task Manage(
            VirtualMachineResource virtualMachine,
            string subscriptionId,
            CancellationToken stoppingToken);
    }
}