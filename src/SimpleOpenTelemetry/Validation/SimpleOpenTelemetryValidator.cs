using OpenTelemetry.Resources;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.Validation;

public static class SimpleOpenTelemetryValidator
{
    private const string EventCategory = nameof(SimpleOpenTelemetryValidator);

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
    /// <param name="host"></param>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static bool Validate(object? host, Resource? resource)
    {
        if (host is null)
        {
            EventSource.Log.Error(EventCategory,
                "OpenTelemetry has not been registered. " +
                "Ensure AddSimpleOpenTelemetry() is called with a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
            return false;
        }

        if (resource == null || resource.Attributes.Count() == 0)
        {
            EventSource.Log.Error(EventCategory,
                "No OpenTelemetry signal providers have been registered. " +
                "Ensure a valid SimpleOpenTelemetry configuration section containing at least one Trace, Log or Metric subsection.");
            return false;
        }

        var resourceAttributes = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

        var requiredKeys = new[] { "service.name", "service.namespace", "service.version", "deployment.environment.name" };
        var missing = requiredKeys.Where(k => !resourceAttributes.ContainsKey(k) || string.IsNullOrEmpty(resourceAttributes[k]?.ToString())).ToList();

        if (missing.Any())
        {
            EventSource.Log.Error(EventCategory,
                $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
                "Check OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars / appsettings.json.");
            return false;
        }

        return true;
    }
}