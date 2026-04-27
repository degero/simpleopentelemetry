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
    
}
