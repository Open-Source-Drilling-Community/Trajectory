using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class TrajectoryWebPagesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Trajectory WebPages services. Hosts may register an
    /// <see cref="OSDC.Drilling.Trajectory.WebPages.ITrajectoryReferenceDataCache"/>
    /// before this method to replace the default on-demand provider.
    /// </summary>
    public static IServiceCollection AddTrajectoryWebPages(this IServiceCollection services)
    {
        services.TryAddSingleton<
            OSDC.Drilling.Trajectory.WebPages.ITrajectoryAPIUtils,
            OSDC.Drilling.Trajectory.WebPages.TrajectoryAPIUtils>();
        services.TryAddScoped<
            OSDC.Drilling.Trajectory.WebPages.ITrajectoryReferenceDataCache,
            OSDC.Drilling.Trajectory.WebPages.DirectTrajectoryReferenceDataProvider>();
        return services;
    }
}
