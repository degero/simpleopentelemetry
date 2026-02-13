namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Builder;

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

        var builder = new SimpleOpenTelemetryBuilder();
        configure(builder);

        var tracerProvider = builder.Build();
        services.AddSingleton(tracerProvider);

        return services;
    }
}
