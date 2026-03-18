namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Builder;
using OpenTelemetry;

/// <summary>
/// Extension methods for adding OpenTelemetry to service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config into OpenTelemetry instrumentation
    /// logging and exporting setups.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <returns>The service collection</returns>
    public static ISimpleOpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var otelBuilder = services.AddOpenTelemetry();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);

        builder.Configure();

        return builder;
    }
}
