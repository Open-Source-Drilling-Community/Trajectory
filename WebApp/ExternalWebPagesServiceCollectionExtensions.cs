using Microsoft.Extensions.DependencyInjection;

namespace NORCE.Drilling.Trajectory.WebApp;

public static class ExternalWebPagesServiceCollectionExtensions
{
    public static IServiceCollection AddExternalWebPages(this IServiceCollection services, WebPagesHostConfiguration configuration)
    {
        services.AddSingleton<NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureAPIUtils,
            NORCE.Drilling.WellBoreArchitecture.WebPages.WellBoreArchitectureAPIUtils>();
        services.AddSingleton<NORCE.Drilling.Rig.WebPages.IRigWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.Rig.WebPages.IRigAPIUtils,
            NORCE.Drilling.Rig.WebPages.RigAPIUtils>();
        services.AddScoped<NORCE.Drilling.Rig.WebPages.Shared.RigApiClient>();
        services.AddScoped<NORCE.Drilling.Rig.WebPages.Shared.FieldClusterApiClient>();
        services.AddSingleton<NORCE.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.WellBore.WebPages.IWellBoreAPIUtils,
            NORCE.Drilling.WellBore.WebPages.WellBoreAPIUtils>();
        services.AddSingleton<NORCE.Drilling.Well.WebPages.IWellWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.Well.WebPages.IWellAPIUtils,
            NORCE.Drilling.Well.WebPages.WellAPIUtils>();
        services.AddSingleton<NORCE.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.Cluster.WebPages.IClusterAPIUtils,
            NORCE.Drilling.Cluster.WebPages.ClusterAPIUtils>();
        services.AddSingleton<NORCE.Drilling.Field.WebPages.IFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.Field.WebPages.IFieldAPIUtils,
            NORCE.Drilling.Field.WebPages.FieldAPIUtils>();
        services.AddSingleton<NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionAPIUtils,
            NORCE.Drilling.CartographicProjection.WebPages.CartographicProjectionAPIUtils>();
        services.AddSingleton<NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumAPIUtils,
            NORCE.Drilling.GeodeticDatum.WebPages.GeodeticDatumAPIUtils>();
        services.AddSingleton<NORCE.Drilling.SurveyInstrument.WebPages.ISurveyInstrumentWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.SurveyInstrument.WebPages.ISurveyInstrumentAPIUtils,
            NORCE.Drilling.SurveyInstrument.WebPages.SurveyInstrumentAPIUtils>();
        services.AddSingleton<NORCE.Drilling.EarthGeomagneticField.WebPages.IEarthGeomagneticFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.EarthGeomagneticField.WebPages.IEarthGeomagneticFieldAPIUtils,
            NORCE.Drilling.EarthGeomagneticField.WebPages.EarthGeomagneticFieldAPIUtils>();
        services.AddSingleton<NORCE.Drilling.GravitationalField.WebPages.IGravitationalFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.GravitationalField.WebPages.IGravitationalFieldAPIUtils,
            NORCE.Drilling.GravitationalField.WebPages.APIUtils>();
        services.AddSingleton<NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumWebPageConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumAPIUtils,
            NORCE.Drilling.VerticalDatum.WebPage.APIUtils>();
        return services;
    }
}
