using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Validation;

namespace SimpleOpenTelemetry;

/// <summary>
/// OpenTelemetry setup for applications that don't support IHostApplicationBuilder Generic Host
/// </summary>
public static class SimpleOpenTelemetryBootstrap
{
    public static OpenTelemetrySdk? Sdk { get; private set; }

    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config (in section 'SimpleOpenTelemetry') into OpenTelemetry instrumentation
    /// logging and exporting etc setups.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns>OpenTelemetrySdk for any additional code based configuration</returns>
    public static OpenTelemetrySdk Add(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Always return an OpenTelemetrySdk regardless
        // to not disrupt any custom code dependencies in app
        var sdk = OpenTelemetrySdk.Create(otelBuilder =>
        {
            // This is needed for the OpenTelemetry SDK to pick up
            // configuration from the appsettings.json IConfiguration
            // setting values in env vars will override these. As it cannot be assumed it has been done
            // in end user code.
            foreach (var kvp in configuration.AsEnumerable()
                .Where(kvp => kvp.Value is not null &&         // has a value (not a section)
                        kvp.Key.StartsWith("OTEL_") &&         // ONLY OTEL_ settings
                        !kvp.Key.Contains(':'))) // top-level only (no nested keys)
            {
                if (Environment.GetEnvironmentVariable(kvp.Key) is null)
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }

            // Only run SimpleOpenTelemetryBuilder if basic config fields set. Error Events logged if not set
            if (SimpleOpenTelemetryBuilder.ValidateConfigurationFormat(configuration))
            {
                // Initialize a builder and configure as a Generic host app would
                var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, configuration);
                builder.Configure();
            }
        });
        Sdk = sdk;
        return sdk;
    }

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
    /// <returns>True if valid</returns>
    public static bool SimpleOpenTelemetryValidate(OpenTelemetrySdk? sdk = null)
    {
        var sdkToVerify = sdk ?? SimpleOpenTelemetryBootstrap.Sdk;

        // If OpenTelemetrySdk.Create() is called these are always initialized as empty
        var resource = new[]
        {
            sdkToVerify?.TracerProvider?.GetResource(),
            sdkToVerify?.MeterProvider?.GetResource(),
            sdkToVerify?.LoggerProvider?.GetResource()
        }
        .FirstOrDefault(r => r?.Attributes.Count() > 0);

        return SimpleOpenTelemetryValidator.Validate(sdkToVerify, resource);
    }

    internal static void Shutdown()
    {
        Sdk?.Dispose();
        Sdk = null;
    }

}
