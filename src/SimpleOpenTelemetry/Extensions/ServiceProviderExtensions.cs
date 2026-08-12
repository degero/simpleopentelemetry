using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Validation;
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
            EventSource.Log.CriticalEvent(EventCategory, "services argument is null.");
            return false;
        }

        // Check opentelemetry registered
        var hostedServices = services.GetServices<IHostedService>();
        var telemetryHost = hostedServices.Count() > 0 ? hostedServices.FirstOrDefault(r => r.GetType().Name.Contains("TelemetryHostedService")) : null;

        // Check at least one signal output by getting resource from available providers (TracerProvider, MeterProvider, or LoggerProvider)
        var resource = GetResourceFromProviders(services);


        return SimpleOpenTelemetryValidator.Validate(telemetryHost, resource);
    }

    /// <summary>
    /// Retrieves the OpenTelemetry resource from the first available provider (TracerProvider, MeterProvider, or LoggerProvider).
    /// </summary>
    /// <param name="services">The service provider instance.</param>
    /// <returns>The resource if found, otherwise null.</returns>
    private static OpenTelemetry.Resources.Resource? GetResourceFromProviders(IServiceProvider services)
    {
        OpenTelemetry.Resources.Resource?[] candidates =
        [
            services.GetService<TracerProvider>()?.GetResource(),
            services.GetService<MeterProvider>()?.GetResource(),
            services.GetService<LoggerProvider>()?.GetResource(),
        ];

        return candidates.FirstOrDefault(r => r is not null && r.Attributes.Any());
    }
}
