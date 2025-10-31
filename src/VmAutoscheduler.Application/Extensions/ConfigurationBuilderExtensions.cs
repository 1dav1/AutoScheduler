using Microsoft.Extensions.Configuration;

namespace VmAutoscheduler.Application.Extensions
{
    internal static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            return configurationBuilder;
        }
    }
}
