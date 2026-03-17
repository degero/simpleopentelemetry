namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Builder;
using OpenTelemetry;

/// <summary>
/// Extension methods for adding SimpleOpenTelemetry to service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to process env var / json config into OpenTelemetry instrumentation logging and exporting
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="otelBuilder">Open telemetry builder</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <returns>The service collection</returns>
    public static ISimpleOpenTelemetryBuilder SimpleOpenTelemetry(
        this IServiceCollection services,
        OpenTelemetryBuilder otelBuilder,
        IConfiguration configuration)
    {
        if (services == null) 
            throw new ArgumentNullException(nameof(services));

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);

        builder.Configure();

        return builder;
    }
}
