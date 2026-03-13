namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;
using OpenTelemetry;
using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for adding SimpleOpenTelemetry to service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleOpenTelemetry to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="otelBuilder">Open telemetry builder</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection</returns>
    public static ISimpleOpenTelemetryBuilder ConfigureOpenTelemetry(
        this IServiceCollection services,
        OpenTelemetryBuilder otelBuilder,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder);

        // var options = new SimpleOpenTelemetryConfiguration();
        // configuration.Bind(options);
        builder.ConfigureExporterFromOptions(configuration);

        return builder;
    }

    /// <summary>
    /// Adds SimpleOpenTelemetry to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="otelBuilder">Open telemetry builder</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection</returns>
    public static ISimpleOpenTelemetryBuilder ConfigureOpenTelemetry(
        this IServiceCollection services,
        OpenTelemetryBuilder otelBuilder,
        Action<ISimpleOpenTelemetryBuilder>? configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder);

        if (configure != null)
            configure(builder);

        return builder;
    }
}
