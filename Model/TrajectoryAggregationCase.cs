using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregationCase : TrajectoryAggregationCaseLight
    {
        public const double DefaultEpsilonL = 250.0;
        public const double DefaultEpsilonKappa = 0.65 * Math.PI / (30.0 * 180.0);
        public const double DefaultAlpha = 0.9;
        public const double DefaultInterpolationInterval = 10.0;
        public const double DefaultDistanceReferenceCoarseningThreshold = 0.1;

        public TrajectoryAggregationCase()
        {
        }

        public bool Calculate(Func<Guid, Trajectory?> trajectoryProvider, Action<double, string?>? progress = null)
        {
            if (TrajectoryAggregationList is not { Count: > 0 })
            {
                CalculationMessage = "No trajectory was selected.";
                return false;
            }

            double epsilonL = PositiveOrDefault(EpsilonL, DefaultEpsilonL);
            double epsilonKappa = PositiveOrDefault(EpsilonKappa, DefaultEpsilonKappa);
            double alpha = PositiveOrDefault(Alpha, DefaultAlpha);
            double interpolationInterval = PositiveOrDefault(InterpolationInterval, DefaultInterpolationInterval);
            double coarseningThreshold = NonNegativeOrDefault(DistanceReferenceCoarseningThreshold, DefaultDistanceReferenceCoarseningThreshold);

            int completed = 0;
            bool anyCompleted = false;
            foreach (TrajectoryAggregation aggregation in TrajectoryAggregationList)
            {
                if (aggregation.ID == Guid.Empty)
                {
                    aggregation.ID = Guid.NewGuid();
                }

                aggregation.CalculationState = CalculationState.Running;
                aggregation.CalculationProgress = 0.0;
                aggregation.CalculationMessage = "Preparing trajectory";
                progress?.Invoke((completed + 0.02) / TrajectoryAggregationList.Count, aggregation.CalculationMessage);

                Trajectory? trajectory = trajectoryProvider(aggregation.TrajectoryID);
                if (trajectory == null)
                {
                    MarkFailed(aggregation, "Trajectory was not found.");
                    completed++;
                    continue;
                }

                bool success = TrajectoryAggregationCalculator.Calculate(
                    trajectory,
                    aggregation,
                    epsilonL,
                    epsilonKappa,
                    alpha,
                    interpolationInterval,
                    coarseningThreshold,
                    (childProgress, message) =>
                    {
                        aggregation.CalculationProgress = Math.Clamp(childProgress, 0.0, 1.0);
                        aggregation.CalculationMessage = message;
                        progress?.Invoke((completed + aggregation.CalculationProgress) / TrajectoryAggregationList.Count, message);
                    });

                if (success)
                {
                    aggregation.CalculationState = CalculationState.Completed;
                    aggregation.CalculationProgress = 1.0;
                    aggregation.CalculationMessage = null;
                    anyCompleted = true;
                }
                else
                {
                    MarkFailed(aggregation, aggregation.CalculationMessage ?? "Trajectory aggregation failed.");
                }

                completed++;
                progress?.Invoke((double)completed / TrajectoryAggregationList.Count, aggregation.CalculationMessage);
            }

            CalculationState = anyCompleted && TrajectoryAggregationList.All(x => x.CalculationState == CalculationState.Completed)
                ? CalculationState.Completed
                : CalculationState.Failed;
            CalculationProgress = 1.0;
            CalculationMessage = CalculationState == CalculationState.Completed ? null : "One or more trajectory aggregations failed.";
            return anyCompleted;
        }

        private static void MarkFailed(TrajectoryAggregation aggregation, string message)
        {
            aggregation.CalculationState = CalculationState.Failed;
            aggregation.CalculationProgress = 0.0;
            aggregation.CalculationMessage = message;
        }

        private static double PositiveOrDefault(double? value, double defaultValue) =>
            value is double defined && IsDefined(defined) && defined > 0.0 ? defined : defaultValue;

        private static double NonNegativeOrDefault(double? value, double defaultValue) =>
            value is double defined && IsDefined(defined) && defined >= 0.0 ? defined : defaultValue;

        private static bool IsDefined(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
