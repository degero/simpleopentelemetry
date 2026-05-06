using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for validating OpenTelemetry configuration on IServiceProvider.
/// </summary>
public static class ServiceProviderExtensions
{
    private const string EventCategory = nameof(SimpleOpenTelemetryValidate);

    /// <summary>
    /// Validates that all key OpenTelemetry resource attributes and servicename are configured and at least one 
    /// signal type (trace/log/metric) OpenTelemetry provider has been set via SimpleOpenTelemetry configuration.
    /// Writes errors via EventSource for any misconfiguration issues found.
    /// </summary>
    /// <remarks>
    /// For validation to pass, set values via OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES environment variables / appsettings.json.
    /// 
    /// Required OTEL_RESOURCE_ATTRIBUTES: service.version, service.namespace, deployment.environment.name
    /// This method checks TracerProvider, MeterProvider, and LoggerProvider for the resource.
    /// At least one of these providers must be registered and contain valid resource attributes.
    /// </remarks>
    /// <param name="services">The service provider instance.</param>
    /// <returns>True if valid</returns>
    public static bool SimpleOpenTelemetryValidate(this IServiceProvider services)
    {
        if (services is null)
        {
            EventSource.Log.Error(EventCategory, "services argument is null.");
            return false;
        }

        // Check opentelemetry registered
        var hostedServices = services.GetServices<IHostedService>();
        var telemetryHost = hostedServices.Count() > 0 ? hostedServices.First(r => r.GetType().Name.Contains("TelemetryHostedService")) : null;
        if (telemetryHost is null)
        {
            EventSource.Log.Error(EventCategory,
                "OpenTelemetry has not been registered. " +
                "Ensure AddSimpleOpenTelemetry() is called with a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
            return false;
        }

        // Check at least one signal output by getting resource from available providers (TracerProvider, MeterProvider, or LoggerProvider)
        var resource = GetResourceFromProviders(services);

        if (resource == null)
        {
            EventSource.Log.Error(EventCategory,
                "No OpenTelemetry signal providers have been registered. " +
                "Ensure a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
            return false;
        }

        var attrs = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

        var requiredKeys = new[] { "service.name", "service.namespace", "service.version", "deployment.environment.name" };
        var missing = requiredKeys.Where(k => !attrs.ContainsKey(k) || string.IsNullOrEmpty(attrs[k]?.ToString())).ToList();

        if (missing.Any())
        {
            EventSource.Log.Error(EventCategory,
                $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
                "Check OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars / appsettings.json.");
            return false;
        }
        return true;
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