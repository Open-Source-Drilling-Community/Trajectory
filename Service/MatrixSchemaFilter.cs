using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using OSDC.DotnetLibraries.General.Math;
namespace OSDC.Drilling.Trajectory.Service;
public sealed class MatrixSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(SymmetricMatrix3x3) && context.Type != typeof(Matrix3x3)) return;
        schema.Properties["FormatVersion"] = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 1, Description = "Version 1 lossless matrix. Missing Values denotes unavailable legacy matrix entries." };
        schema.Properties["Values"] = new OpenApiSchema { Type = "array", MinItems = 9, MaxItems = 9, Items = new OpenApiSchema { Type = "number", Format = "double", Nullable = true }, Description = "Nine row-major entries. Covariance uses square metres in north/east/depth axes; eigenvector entries are dimensionless. Legacy dimension-only payloads remain readable." };
    }
}
