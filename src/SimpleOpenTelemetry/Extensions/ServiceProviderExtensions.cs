using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
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
    /// 
    /// This method checks TracerProvider, MeterProvider, and LoggerProvider for the resource.
    /// At least one of these providers must be registered and contain valid resource attributes.
    /// </remarks>
    /// <param name="services">The service provider instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when OpenTelemetry is not configured or required resource attributes are missing or empty.</exception>
    public static void SimpleOpenTelemetryValidate(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Check opentelemetry registered
        var hostedServices = services.GetServices<IHostedService>();
        var telemetryHost = hostedServices.Count() > 0 ? hostedServices.Where(r => r.GetType().Name.Contains("TelemetryHostedService")).First() : null;
        if (telemetryHost is null)
              throw new InvalidOperationException(
                "OpenTelemetry has not been registered. " +
                "Ensure AddSimpleOpenTelemetry() is called with a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
      
        // Check at least one signal output by getting resource from available providers (TracerProvider, MeterProvider, or LoggerProvider)
        var resource = GetResourceFromProviders(services);

        if (resource == null)
        {
            throw new InvalidOperationException(
                "No OpenTelemetry signal providers have been registered. " +
                "Ensure a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
        }

        var attrs = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

        // TODO Chad change to best practice perhaps on prod build? - service.name, service.namespace, service.version, service.instance.id, host.name, host.type, os.name, and os.version
        var requiredKeys = new[] { "service.name", "service.version", "deployment.environment.name" }; //"service.instance.id","host.name", "host.type", "os.type", "os.version"
        var missing = requiredKeys.Where(k => !attrs.ContainsKey(k) || string.IsNullOrEmpty(attrs[k]?.ToString())).ToList();

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
                "Check OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars / appsettings.json.");
        }
    }

    /// <summary>
    /// Retrieves the OpenTelemetry resource from the first available provider (TracerProvider, MeterProvider, or LoggerProvider).
    /// </summary>
    /// <param name="services">The service provider instance.</param>
    /// <returns>The resource if found, otherwise null.</returns>
    private static OpenTelemetry.Resources.Resource? GetResourceFromProviders(IServiceProvider services)
    {
        // Try TracerProvider first
        var tracerProvider = services.GetService<TracerProvider>();
        if (tracerProvider != null)
            return tracerProvider.GetResource();

        // Try MeterProvider
        var meterProvider = services.GetService<MeterProvider>();
        if (meterProvider != null)
            return meterProvider.GetResource();

        // Try LoggerProvider
        var loggerProvider = services.GetService<LoggerProvider>();
        if (loggerProvider != null)
            return loggerProvider.GetResource();

        return null;
    }
}
