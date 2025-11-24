using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;

namespace Quartz.Job.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    private static readonly NameValueCollection Properties = new()
    {
        ["quartz.scheduler.interruptJobsOnShutdownWithWait"] = "true",
        ["org.quartz.scheduler.makeSchedulerThreadDaemon"] = "true",
        ["quartz.plugin.shutdownhook.type"] = "Quartz.Plugin.Management.ShutdownHookPlugin, Quartz.Plugins",
        ["quartz.plugin.shutdownhook.cleanShutdown"] = "true",
        ["quartz.threadPool.maxConcurrency"] = "1000"
    };

    /// <summary>
    /// Adds Quartz services to the application
    /// <para>
    /// To schedule a job from the application settings, you can also use <see cref="IServiceCollectionQuartzConfiguratorExtensions.ConfigureJobs(IServiceCollectionQuartzConfigurator, Microsoft.Extensions.Configuration.IConfiguration)"/>
    /// </para>
    /// </summary>
    public static IServiceCollection AddQuartzServices(
        this IServiceCollection services,
        Action<IServiceCollectionQuartzConfigurator> configure,
        Dictionary<string, string>? customProperties = null)
    {
        NameValueCollection properties = Properties;
        if (customProperties is { Count: > 0 })
        {
            foreach (var property in customProperties)
                properties[property.Key] = property.Value;
        }

        services.AddQuartz(properties, configure);
        services.AddQuartzHostedService(config => config.WaitForJobsToComplete = true);

        return services;
    }
}