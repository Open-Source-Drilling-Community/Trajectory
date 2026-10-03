using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Resumes durable extrapolation calculations interrupted by a service restart.</summary>
public sealed class TrajectoryExtrapolationRecoveryService(
    ILogger<TrajectoryExtrapolationRecoveryService> logger,
    ILogger<TrajectoryExtrapolationCaseManager> managerLogger,
    ILogger<TargetLandingCaseManager> targetLandingManagerLogger,
    SqlConnectionManager mainDatabase) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

        try
        {
            int resumed = TrajectoryExtrapolationCaseManager
                .GetInstance(managerLogger, mainDatabase)
                .ResumeInterruptedCalculations();
            logger.LogInformation("Resumed {CalculationCount} interrupted trajectory extrapolation calculations", resumed);
            int resumedTargetLanding = TargetLandingCaseManager
                .GetInstance(targetLandingManagerLogger, mainDatabase)
                .ResumeInterruptedCalculations();
            logger.LogInformation("Resumed {CalculationCount} interrupted target landing calculations", resumedTargetLanding);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to resume interrupted trajectory extrapolation calculations");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
