using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>
/// Runs durable target-landing calculations outside HTTP request threads. Queued and running
/// records are persisted and queued again when the service starts after an interruption.
/// </summary>
public sealed class TargetLandingCalculationWorker : BackgroundService
{
    private readonly Channel<CalculationRequest> queue_ = Channel.CreateUnbounded<CalculationRequest>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly TargetLandingCaseManager manager_;
    private readonly ILogger<TargetLandingCalculationWorker> logger_;

    public TargetLandingCalculationWorker(
        ILogger<TargetLandingCalculationWorker> logger,
        ILogger<TargetLandingCaseManager> managerLogger,
        SqlConnectionManager connectionManager)
    {
        logger_ = logger;
        manager_ = TargetLandingCaseManager.GetInstance(managerLogger, connectionManager);
    }

    public void Queue(Guid id, DateTimeOffset revision) =>
        queue_.Writer.TryWrite(new CalculationRequest(id, revision));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // BackgroundService.ExecuteAsync runs synchronously during host startup until its first
        // incomplete await on .NET 8. Yield before database recovery so Kestrel can start.
        await Task.Yield();

        List<(Guid Id, DateTimeOffset Revision)> interrupted = manager_.PrepareInterruptedCalculationsForResume();
        foreach ((Guid id, DateTimeOffset revision) in interrupted)
            Queue(id, revision);
        logger_.LogInformation("Queued {CalculationCount} interrupted target landing calculations", interrupted.Count);

        await foreach (CalculationRequest request in queue_.Reader.ReadAllAsync(stoppingToken))
            await manager_.RecalculateAsync(request.Id, request.Revision, stoppingToken);
    }

    private readonly record struct CalculationRequest(Guid Id, DateTimeOffset Revision);
}
