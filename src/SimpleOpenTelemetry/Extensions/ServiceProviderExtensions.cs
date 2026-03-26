using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for validating SimpleOpenTelemetry configuration on IServiceProvider.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Validates that all required OpenTelemetry resource attributes are configured.
    /// </summary>
    /// <remarks>
    /// Required attributes: service.name, service.version, deployment.environment.name.
    /// This method will throw an InvalidOperationException if any required attributes are missing or empty.
    /// Useful for production environments to ensure proper telemetry identification.
    /// Set values via OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES environment variables.
    /// </remarks>
    /// <param name="services">The service provider instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required resource attributes are missing or empty.</exception>
    public static void SimpleOpenTelemetryValidate(this IServiceProvider services)
	{
        var tracerProvider = services.GetRequiredService<TracerProvider>();
        var resource = tracerProvider.GetResource();

        var attrs = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

        // TODO Chad change to best practice perhaps on prod build? - service.name, service.namespace, service.version, service.instance.id, host.name, host.type, os.name, and os.version
        var requiredKeys = new[] { "service.name", "service.version", "deployment.environment.name" }; //"service.instance.id","host.name", "host.type", "os.type", "os.version"
        var missing = requiredKeys.Where(k => !attrs.ContainsKey(k) || string.IsNullOrEmpty(attrs[k]?.ToString())).ToList();

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
                "Set OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars.");
        }

    }
}
