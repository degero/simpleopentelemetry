using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry;

/// <summary>
/// OpenTelemetry setup for applications that dont support IHostApplicationBuilder Generic Host
/// </summary>
public static class NonGenericHostEntry
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config (in section 'SimpleOpenTelemetry') into OpenTelemetry instrumentation
    /// logging and exporting etc setups.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static OpenTelemetrySdk AddSimpleOpenTelemetry(
        IConfiguration configuration)
    {
        return OpenTelemetrySdk.Create(otelBuilder =>
        {
            var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);
            builder.Configure();
        });
    }
}
