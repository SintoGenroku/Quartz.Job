using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Quartz.Job.Attributes;
using Quartz.Job.Listeners;
using Quartz.Job.Options;

namespace Quartz.Job.Extensions.DependencyInjection;

// ReSharper disable once InconsistentNaming
public static class IServiceCollectionQuartzConfiguratorExtensions
{
    /// <summary>
    /// Настраивает Listener для заданий
    /// </summary>
    public static void ConfigureJobListener<TListener>(this IServiceCollectionQuartzConfigurator quartz,
        params IMatcher<JobKey>[] matchers) where TListener : class, IJobListener
        => quartz.AddJobListener<TListener>(matchers);

    /// <summary>
    /// Настраивает Listener для триггеров
    /// </summary>
    public static void ConfigureTriggerListener<TListener>(this IServiceCollectionQuartzConfigurator quartz,
        params IMatcher<TriggerKey>[] matchers) where TListener : class, ITriggerListener
        => quartz.AddTriggerListener<TListener>(matchers);

    /// <summary>
    /// Настраивает Listener для триггеров
    /// </summary>
    public static void ConfigureTriggerListener<TListener>(this IServiceCollectionQuartzConfigurator quartz,
        Func<IServiceProvider, TListener> factory,
        params IMatcher<TriggerKey>[] matchers) where TListener : class, ITriggerListener
        => quartz.AddTriggerListener(factory, matchers);

    /// <summary>
    /// Настраивает Listener для планировщика
    /// </summary>
    public static void ConfigureSchedulerListener<TListener>(this IServiceCollectionQuartzConfigurator quartz) where TListener : class, ISchedulerListener
        => quartz.AddSchedulerListener<TListener>();

    /// <summary>
    /// Планирует Job, используя настройки приложения
    /// </summary>
    public static void ConfigureJobs(this IServiceCollectionQuartzConfigurator quartz, IConfiguration configuration)
    {
        const string sectionName = "QuartzJobs";

        IConfiguration section = configuration is IConfigurationRoot root ? root.GetSection(sectionName) : configuration;
        QuartzJobsOptions? options = section.Get<QuartzJobsOptions>();
        if (options == null || !options.Any())
        {
            return;
        }

        AssemblyName[] assemblies = Assembly.GetEntryAssembly()!
            .GetReferencedAssemblies()
            .Where(e => e.Name != null && !e.Name.StartsWith("Microsoft.") && !e.Name.StartsWith("System."))
            .ToArray();

        foreach (var assembly in assemblies)
        {
            try
            {
                // ReSharper disable once SimplifyLinqExpressionUseAll
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(e => e.FullName == assembly.FullName))
                    Assembly.Load(assembly);
            }
            catch
            {
                // Suppress exception
            }
        }

        quartz.ConfigureTriggerListener<DefaultTriggerListener>();

        foreach (var (name, jobOptions) in options)
        {
            if (!TryGetType(name, out Type? type))
                continue;

            if (!jobOptions.FireAndForget)
            {
                if (string.IsNullOrWhiteSpace(jobOptions.CronSchedule) ||
                    new CronExpression(jobOptions.CronSchedule).GetNextValidTimeAfter(DateTimeOffset.UtcNow) == null)
                    continue;
            }

            string prefix = type.GetCustomAttribute<PreloadJobAttribute>(true) != null
                ? Consts.PreloadPrefix
                : string.Empty;

            string key = type.Name;
            for (int i = 0; i < jobOptions.InstanceCount; i++)
            {
                string groupName = $"{prefix}{key}Group{i + 1:D2}";

                quartz.AddJob(type, null, config => ConfigureJob(config, key, groupName, jobOptions.Data));

                string triggerName = $"{prefix}{key}Trigger";

                if (jobOptions.FireAndForget)
                {
                    quartz.AddTrigger(trigger => trigger.ForJob(key, groupName)
                        .WithIdentity(triggerName, groupName));
                }
                else
                {
                    quartz.AddTrigger(trigger => trigger.ForJob(key, groupName)
                        .WithIdentity(triggerName, groupName)
                        .WithCronSchedule(jobOptions.CronSchedule, cron => cron.WithMisfireHandlingInstructionDoNothing()));
                }
            }
        }
    }

    private static void ConfigureJob(IJobConfigurator config,
        string key,
        string groupName,
        Dictionary<string, string> data)
    {
        config.WithIdentity(key, groupName);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data is { Count: > 0 })
        {
            foreach (var option in data)
            {
                config.UsingJobData(option.Key, option.Value);
            }
        }
    }

    private static bool TryGetType(string name, [NotNullWhen(true)] out Type? type)
    {
        type = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(e => !string.IsNullOrWhiteSpace(e.FullName) && !e.FullName.StartsWith("Microsoft") && !e.FullName.StartsWith("System"))
            .SelectMany(e => e.GetTypes())
            .SingleOrDefault(e => e.Name == name || e.FullName == name);

        return type != null;
    }
}