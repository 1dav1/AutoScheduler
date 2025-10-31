using VmAutoscheduler.Application.Models;

namespace VmAutoscheduler.Application.Infrastructure;

public interface ICsvWriter
{
    void Write(VirtualMachine virtualMachine);
}
