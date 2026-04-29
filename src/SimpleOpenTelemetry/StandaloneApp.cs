using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry;

/// <summary>
/// OpenTelemetry setup for applications that don't support IHostApplicationBuilder Generic Host
/// </summary>
public static class StandaloneApp
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config (in section 'SimpleOpenTelemetry') into OpenTelemetry instrumentation
    /// logging and exporting etc setups.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns>OpenTelemetrySdk for any additional code based configuration</returns>
    public static OpenTelemetrySdk AddSimpleOpenTelemetry(
        IConfiguration configuration)
    {
        return OpenTelemetrySdk.Create(otelBuilder =>
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
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }

            // Initialize a builder and configure as a Generic host app would
            var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);
            builder.Configure();
        });
    }
}
