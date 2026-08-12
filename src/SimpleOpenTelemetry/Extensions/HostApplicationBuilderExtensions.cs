using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for configuring SimpleOpenTelemetry with IHostApplicationBuilder.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry to the host application builder with settings from
    /// env var / json config (in root section 'SimpleOpenTelemetry')
    /// </summary>
    /// <remarks>
    /// This method configures many components / settings of OpenTelemetry.
    /// Configuration is loaded from the application's configuration (e.g., appsettings.json).
    /// </remarks>
    /// <param name="builder">The host application builder instance.</param>
    /// <returns cref="OpenTelemetryBuilder">OpenTelemetry builder that this method creates</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    public static OpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddSimpleOpenTelemetry(builder.Configuration);
    }
}
