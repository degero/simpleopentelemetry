// ConfigurationHelper.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Configuration;

public static class OpenTelemetryConfigurationHelper
{
    /// <summary>
    /// Loads SimpleOpenTelemetryConfiguration from configuration
    /// Works with any IConfiguration instance (console, lib, service, web, etc.)
    /// </summary>
    public static SimpleOpenTelemetryConfiguration GetOpenTelemetryOptions(
        this IConfiguration configuration)
    {
        return configuration
            .GetSection(SimpleOpenTelemetryConfiguration.SectionName)
            .Get<SimpleOpenTelemetryConfiguration>() ?? new SimpleOpenTelemetryConfiguration();
    }

    /// <summary>
    /// Registers SimpleOpenTelemetryConfiguration with DI container (optional, for DI-enabled apps)
    /// </summary>
    public static IServiceCollection AddOpenTelemetryOptions(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.Configure<SimpleOpenTelemetryConfiguration>(
            configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName));

        return services;
    }

    /// <summary>
    /// Loads and registers options in one call (convenience method for DI-enabled apps)
    /// </summary>
    public static SimpleOpenTelemetryConfiguration LoadOpenTelemetryOptions(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddOpenTelemetryOptions(configuration);
        return configuration.GetOpenTelemetryOptions();
    }
}