namespace Quartz.Job.Options;

/// <summary>
/// Model of settings for a specific Job Quartz
/// </summary>
public sealed class QuartzJobOptions
{
    /// <summary>
    /// Number of copies
    /// </summary>
    public int InstanceCount { get; set; } = 0;

    /// <summary>
    /// Indicates that the job will be run once
    /// </summary>
    public bool FireAndForget { get; set; } = false;

    /// <summary>
    /// Cron expression for scheduling launches
    /// <para>
    /// You can create and check it on the website
    /// <see href="https://www.freeformatter.com/cron-expression-generator-quartz.html"/>
    /// </para>
    /// </summary>
    public string CronSchedule { get; set; } = string.Empty;

    /// <summary>
    /// Additional settings that will be passed via JobData
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();
}
