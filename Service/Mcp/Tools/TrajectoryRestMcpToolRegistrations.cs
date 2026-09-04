using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using OSDC.Drilling.Trajectory.Service.Controllers;

namespace OSDC.Drilling.Trajectory.Service.Mcp.Tools;

public static class TrajectoryRestMcpToolRegistrations
{
    private static readonly NullabilityInfoContext Nullability = new();
    private static readonly Type[] ControllerTypes = typeof(TrajectoryController).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
        .Where(type => type != typeof(TrajectoryUsageStatisticsController))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static IReadOnlyList<TrajectoryMcpEndpoint> Endpoints { get; } = DiscoverEndpoints();

    public static IServiceCollection AddTrajectoryRestMcpTools(this IServiceCollection services)
    {
        foreach (TrajectoryMcpEndpoint endpoint in Endpoints)
        {
            services.AddLegacyMcpTool(endpoint.Name, endpoint.Description, endpoint.InputSchema,
                endpoint.OutputSchema, endpoint.Behavior,
                (sp, arguments, cancellationToken) => InvokeAsync(sp, endpoint, arguments, cancellationToken));
        }
        return services;
    }

    private static IReadOnlyList<TrajectoryMcpEndpoint> DiscoverEndpoints()
    {
        var endpoints = new List<TrajectoryMcpEndpoint>();
        foreach (Type controllerType in ControllerTypes)
        {
            MethodInfo[] actions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(true).Any())
                .OrderBy(method => method.MetadataToken)
                .ToArray();
            foreach (MethodInfo method in actions)
            {
                bool overloaded = actions.Count(candidate => candidate.Name == method.Name) > 1;
                string controllerName = controllerType.Name[..^"Controller".Length];
                string actionName = ToSnakeCase(method.Name);
                if (overloaded && method.GetParameters().Length > 0)
                    actionName += "_by_" + string.Join("_and_", method.GetParameters().Select(parameter => ToSnakeCase(parameter.Name!)));

                string name = $"{ToSnakeCase(controllerName)}_{actionName}";
                string verbs = string.Join('/', method.GetCustomAttributes<HttpMethodAttribute>(true).SelectMany(attribute => attribute.HttpMethods).Distinct());
                string? template = method.GetCustomAttributes(true).OfType<IRouteTemplateProvider>().Select(attribute => attribute.Template).FirstOrDefault(value => value is not null);
                endpoints.Add(new TrajectoryMcpEndpoint(
                    controllerType,
                    method,
                    name,
                    TrajectoryMcpToolMetadata.Describe(controllerName, method, verbs, template),
                    TrajectoryMcpToolMetadata.CreateInputSchema(controllerName, method),
                    TrajectoryMcpToolMetadata.CreateOutputSchema(method),
                    TrajectoryMcpToolMetadata.CreateBehavior(controllerName, method, verbs)));
            }
        }

        string[] duplicateNames = endpoints.GroupBy(endpoint => endpoint.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateNames.Length > 0)
            throw new InvalidOperationException($"Duplicate MCP tool names: {string.Join(", ", duplicateNames)}");
        return endpoints;
    }

    private static async Task<JsonNode?> InvokeAsync(IServiceProvider services, TrajectoryMcpEndpoint endpoint, JsonObject? arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryBindArguments(endpoint.Method, arguments, out object?[] values, out JsonNode? error)) return error;
        object controller = ActivatorUtilities.CreateInstance(services, endpoint.ControllerType);
        object? result;
        try { result = endpoint.Method.Invoke(controller, values); }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        if (result is Task task)
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            result = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")!.GetValue(task) : null;
        }
        return McpActionResultConverter.FromUnknown(result);
    }

    private static bool TryBindArguments(MethodInfo method, JsonObject? arguments, out object?[] values, out JsonNode? error)
    {
        ParameterInfo[] parameters = method.GetParameters();
        values = new object?[parameters.Length];
        error = null;
        string? unexpected = arguments?.Select(item => item.Key)
            .FirstOrDefault(key => parameters.All(parameter => parameter.Name != key));
        if (unexpected is not null)
        {
            error = McpToolResponses.Validation($"Unexpected argument '{unexpected}'.");
            return false;
        }

        for (int index = 0; index < parameters.Length; index++)
        {
            ParameterInfo parameter = parameters[index];
            string name = parameter.Name!;
            if (arguments?[name] is not JsonNode node)
            {
                if (parameter.HasDefaultValue) { values[index] = parameter.DefaultValue; continue; }
                if (IsNullable(parameter))
                {
                    if (parameter.GetCustomAttribute<FromBodyAttribute>() is null) { values[index] = null; continue; }
                }
                error = McpToolResponses.Validation($"Argument '{name}' is required.");
                return false;
            }
            try
            {
                values[index] = node.Deserialize(parameter.ParameterType, JsonSettings.Options);
                if (values[index] is null && parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
                    throw new JsonException();
                if (values[index] is Guid guid && guid == Guid.Empty)
                {
                    error = McpToolResponses.Validation($"Argument '{name}' must be a non-empty UUID.");
                    return false;
                }
                if (name == "id" && values[index] is string text && string.IsNullOrWhiteSpace(text))
                {
                    error = McpToolResponses.Validation("Argument 'id' must not be empty.");
                    return false;
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
            {
                error = McpToolResponses.Validation($"Argument '{name}' could not be deserialized as {parameter.ParameterType.Name}.");
                return false;
            }
        }
        return true;
    }

    private static bool IsNullable(ParameterInfo parameter)
    {
        if (Nullable.GetUnderlyingType(parameter.ParameterType) is not null) return true;
        if (parameter.ParameterType.IsValueType) return false;
        return Nullability.Create(parameter).ReadState is not NullabilityState.NotNull;
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (index > 0 && char.IsUpper(current) && (char.IsLower(value[index - 1]) || (index + 1 < value.Length && char.IsLower(value[index + 1]))))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(current));
        }
        return builder.ToString();
    }
}

public sealed record TrajectoryMcpEndpoint(Type ControllerType, MethodInfo Method, string Name, string Description,
    JsonObject InputSchema, JsonObject OutputSchema, McpToolBehavior Behavior);
