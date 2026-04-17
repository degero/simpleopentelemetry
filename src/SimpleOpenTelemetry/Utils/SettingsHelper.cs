using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.Utils;

/// <summary>
///
/// </summary>
public static class SettingsHelper
{

    public static bool HasSimpleOpenTelemetrySection(IConfiguration configuration, string sectionName)
    {
        var simpleOtelSection = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        return simpleOtelSection.GetSection(sectionName).Exists();
    }

    /// <summary>
    /// Get env var OTEL_SERVICE_NAME or fallback to calling assembly if no setting
    /// </summary>
    public static string? OtelServiceName(IConfiguration config) =>
        GetConfigValue<string?>(config, OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME);

    public static string? OtelResourceAttributes(IConfiguration config) =>
        GetConfigValue<string?>(config, OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES);

    private static T GetConfigValue<T>(IConfiguration config, string key) =>
        config.GetValue<T>(key);
    

    // TODO chad see if these are of any use

    ///// <summary>
    ///// Loads SimpleOpenTelemetryOptions from configuration
    ///// Works with any IConfiguration instance (console, lib, service, web, etc.)
    ///// </summary>
    //public static SimpleOpenTelemetryOptions GetOpenTelemetryOptions(
    //    this IConfiguration configuration)
    //{
    //    return configuration
    //        .GetSection(SimpleOpenTelemetryOptions.SectionName)
    //        .Get<SimpleOpenTelemetryOptions>() ?? new SimpleOpenTelemetryOptions();
    //}

    ///// <summary>
    ///// Registers SimpleOpenTelemetryOptions with DI container (optional, for DI-enabled apps)
    ///// </summary>
    //public static IServiceCollection AddOpenTelemetryOptions(
    //    this IServiceCollection services,
    //    IConfiguration configuration)
    //{
    //    services.Configure<SimpleOpenTelemetryOptions>(
    //        configuration.GetSection(SimpleOpenTelemetryOptions.SectionName));

    //    return services;
    //}

    ///// <summary>
    ///// Loads and registers options in one call (convenience method for DI-enabled apps)
    ///// </summary>
    //public static SimpleOpenTelemetryOptions LoadOpenTelemetryOptions(
    //    this IServiceCollection services,
    //    IConfiguration configuration)
    //{
    //    services.AddOpenTelemetryOptions(configuration);
    //    return configuration.GetOpenTelemetryOptions();
    //}
}
