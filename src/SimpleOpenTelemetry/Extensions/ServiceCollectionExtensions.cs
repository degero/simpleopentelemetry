namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
/// <summary>
/// Extension methods for adding SimpleOpenTelemetry to service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleOpenTelemetry to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSimpleOpenTelemetry(
        this IServiceCollection services,
        Action<ISimpleOpenTelemetryBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var otelBuilder = services.AddOpenTelemetry();
        var builder = new SimpleOpenTelemetryBuilder(otelBuilder);
        configure(builder);
        return services;
    }

    /// <summary>
    /// Adds SimpleOpenTelemetry to the service collection using configuration from IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <param name="configure">Optional additional configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ISimpleOpenTelemetryBuilder>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var options = new SimpleOpenTelemetryConfiguration();
        configuration.Bind(options);

        return AddSimpleOpenTelemetry(services, builder =>
        {
            builder.ConfigureOtlpExporterFromOptions(
                new SimpleOpenTelemetryBuilderOptions
                {
                    EnableTracing = options.EnableTracing,
                    EnableMetrics = options.EnableMetrics,
                    EnableLogging = options.EnableLogging
                }    
            );
            configure?.Invoke(builder);
        });
    }
}
