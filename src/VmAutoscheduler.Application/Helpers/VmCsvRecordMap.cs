using CsvHelper.Configuration;
using VmAutoscheduler.Application.Models;

namespace VmAutoscheduler.Application.Helpers;

public class VmCsvRecordMap : ClassMap<VmCsvRecord>
{
    public VmCsvRecordMap()
    {
        Map(m => m.TimestampUtc).Name("TimestampUtc");
        Map(m => m.SubscriptionId).Name("SubscriptionId");
        Map(m => m.ResourceGroup).Name("ResourceGroup");
        Map(m => m.ComputerName).Name("ComputerName");
        Map(m => m.PowerState).Name("PowerState");
        Map(m => m.Autoshutdown)
            .TypeConverterOption.BooleanValues(true, booleanValues: "1")
            .TypeConverterOption.BooleanValues(false, booleanValues: "0")
            .Name("Autoshutdown");
        Map(m => m.VmStartTimeUtc).Name("VmStartTimeUtc");
    }
}
