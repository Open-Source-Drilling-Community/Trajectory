using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Publishes calculation-case lifecycle semantics on REST operations.</summary>
internal sealed class TrajectorySemanticOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        string? typeName = context.MethodInfo.DeclaringType?.Name;
        if (typeName is null || !typeName.EndsWith("Controller", StringComparison.Ordinal)) return;
        string controller = typeName[..^"Controller".Length];
        if (TrajectoryProviderSemantics.ForOperation(controller, context.MethodInfo) is { } metadata)
            operation.Extensions[SemanticMetadata.ExtensionName] =
                OpenApiAnyFactory.CreateFromJson(metadata.ToJsonString());
    }
}
