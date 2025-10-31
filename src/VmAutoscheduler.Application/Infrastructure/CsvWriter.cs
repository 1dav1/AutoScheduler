using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using VmAutoscheduler.Application.Constants;
using VmAutoscheduler.Application.Enums;
using VmAutoscheduler.Application.Helpers;
using VmAutoscheduler.Application.Models;
using VmAutoscheduler.Application.Settings;

namespace VmAutoscheduler.Application.Infrastructure;

public class CsvWriter : ICsvWriter
{
    private readonly Lock _csvLock = new();
    private readonly SchedulerSettings _options;
    private readonly CsvConfiguration _csvConfig;

    public CsvWriter(IOptionsMonitor<SchedulerSettings> monitor)
    {
        _options = monitor.CurrentValue;
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine,
            HasHeaderRecord = !File.Exists(_options.CsvOutputPath)
        };
    }

    public void Write(VirtualMachine virtualMachine)
    {
        EnsureCsvHeader();
        var record = new VmCsvRecord
        {
            TimestampUtc = virtualMachine.TimestampUtc.ToString("o"),
            SubscriptionId = virtualMachine.SubscriptionId,
            ResourceGroup = virtualMachine.ResourceGroup,
            ComputerName = virtualMachine.ComputerName,
            PowerState =
                Enum.GetName(virtualMachine.PowerState)?.ToLower() ??
                Enum.GetName(PowerState.Unknown)!.ToLower(),
            Autoshutdown = virtualMachine.AutoShutdown,
            VmStartTimeUtc = virtualMachine.StartTimeUtc?.ToString("o") ?? SchedulerConstants.Unknown
        };
        AppendCsvRecord(record);
    }

    private void EnsureCsvHeader()
    {
        lock (_csvLock)
        {
            if (!File.Exists(_options.CsvOutputPath))
            {
                using var writer = new StreamWriter(_options.CsvOutputPath, append: true, encoding: Encoding.UTF8);
                using var csv = new CsvHelper.CsvWriter(writer, _csvConfig);
                csv.Context.RegisterClassMap<VmCsvRecordMap>();
                csv.WriteHeader<VmCsvRecord>();
                csv.NextRecord();
            }
        }
    }

    private void AppendCsvRecord(VmCsvRecord record)
    {
        lock (_csvLock)
        {
            using var writer = new StreamWriter(_options.CsvOutputPath, append: true, encoding: Encoding.UTF8);
            using var csv = new CsvHelper.CsvWriter(writer, _csvConfig);
            csv.Context.RegisterClassMap<VmCsvRecordMap>();
            csv.WriteRecord(record);
            csv.NextRecord();
        }
    }
}
