using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz.Listener;

namespace Quartz.Job.Listeners;

/// <summary>
/// Default Listener for triggers
/// </summary>
internal sealed class DefaultTriggerListener(HealthCheckService? healthCheckService = null) : TriggerListenerSupport
{
    private const long Interval = 20;

    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static readonly ManualResetEventSlim ResetEvent = new(false);

    private long _lastHealthCheck;
    private bool _isNotAllowed = true;

    /// <inheritdoc cref="TriggerListenerSupport.Name" />
    public override string Name => throw new NotImplementedException();

    public override async Task<bool> VetoJobExecution(
        ITrigger trigger,
        IJobExecutionContext context,
        CancellationToken cancellationToken = new())
    {
        if (_isNotAllowed || healthCheckService == null || trigger.Key.Name.StartsWith(Consts.PreloadPrefix))
            return await base.VetoJobExecution(trigger, context, cancellationToken).ConfigureAwait(false);

        if (await Semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            ResetEvent.Reset();

            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    elapsed = now - _lastHealthCheck;

                if (elapsed > Interval)
                {
                    HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                    _isNotAllowed = report.Status != HealthStatus.Healthy;
                    _lastHealthCheck = now;
                }
            }
            finally
            {
                ResetEvent.Set();
                Semaphore.Release();
            }

            return _isNotAllowed;
        }

        ResetEvent.Wait(cancellationToken);

        return _isNotAllowed;
    }
}