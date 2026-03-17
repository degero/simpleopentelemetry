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

    // TODO Chad remove
    /// <summary>
    /// Adds SimpleOpenTelemetry to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="otelBuilder">Open telemetry builder</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection</returns>
    //public static ISimpleOpenTelemetryBuilder SimpleOpenTelemetry(
    //    this IServiceCollection services,
    //    OpenTelemetryBuilder otelBuilder,
    //    Action<ISimpleOpenTelemetryBuilder>? configure,
    //    IConfiguration configuration)
    //{
    //    if (services == null) throw new ArgumentNullException(nameof(services));

    //    var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);

    //    if (configure != null)
    //        configure(builder);

    //    return builder;
    //}
}
