using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for configuring SimpleOpenTelemetry with IHostApplicationBuilder.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds SimpleOpenTelemetry instrumentation and exporters to the host application builder.
    /// </summary>
    /// <remarks>
    /// This method configures logging and services for OpenTelemetry.
    /// Configuration is loaded from the application's configuration (e.g., appsettings.json).
    /// </remarks>
    /// <param name="builder">The host application builder instance.</param>
    /// <returns>The host application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    public static IOpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        builder.Logging.AddSimpleOpenTelemetry();

        // TODO Chad look at way to not pass configuration like AddOpenTelemetry()
        var otelBuilder =builder.Services.AddSimpleOpenTelemetry(builder.Configuration);

        return otelBuilder;
    }

}