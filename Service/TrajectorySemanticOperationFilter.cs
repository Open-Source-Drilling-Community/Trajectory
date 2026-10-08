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
        foreach (var parameter in context.MethodInfo.GetParameters())
            if (TrajectoryProviderSemantics.ForParameter(controller, parameter) is { } parameterMetadata &&
                operation.Parameters.FirstOrDefault(p => p.Name == parameter.Name) is { } target)
            {
                target.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(parameterMetadata.ToJsonString());
                if (target.Schema is not null)
                {
                    var schema = TrajectoryProviderSemantics.IsIdentifierCollection(parameter.ParameterType) && target.Schema.Items is not null ? target.Schema.Items : target.Schema;
                    schema.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(parameterMetadata.ToJsonString());
                }
            }
    }
}
