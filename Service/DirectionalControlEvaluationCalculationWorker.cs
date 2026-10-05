using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Executes durable directional-control evaluations outside HTTP request threads.</summary>
public sealed class DirectionalControlEvaluationCalculationWorker : BackgroundService
{
    private readonly Channel<CalculationRequest> queue_ = Channel.CreateUnbounded<CalculationRequest>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly DirectionalControlEvaluationCaseManager manager_;
    private readonly ILogger<DirectionalControlEvaluationCalculationWorker> logger_;

    public DirectionalControlEvaluationCalculationWorker(
        ILogger<DirectionalControlEvaluationCalculationWorker> logger,
        ILogger<DirectionalControlEvaluationCaseManager> managerLogger,
        SqlConnectionManager connectionManager)
    {
        logger_ = logger;
        manager_ = DirectionalControlEvaluationCaseManager.GetInstance(managerLogger, connectionManager);
    }

    public void Queue(Guid id, DateTimeOffset revision) =>
        queue_.Writer.TryWrite(new CalculationRequest(id, revision));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        List<(Guid Id, DateTimeOffset Revision)> interrupted = manager_.PrepareInterruptedCalculationsForResume();
        foreach ((Guid id, DateTimeOffset revision) in interrupted) Queue(id, revision);
        logger_.LogInformation("Queued {CalculationCount} interrupted directional-control evaluations", interrupted.Count);

        await foreach (CalculationRequest request in queue_.Reader.ReadAllAsync(stoppingToken))
            await manager_.RecalculateAsync(request.Id, request.Revision, stoppingToken);
    }

    private readonly record struct CalculationRequest(Guid Id, DateTimeOffset Revision);
}
