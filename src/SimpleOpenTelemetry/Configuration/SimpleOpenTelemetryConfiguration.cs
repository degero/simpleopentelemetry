using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.Configuration;

/// <summary>
/// Configuration to use with IOptions for applications using SimpleOpenTelemetry 
/// </summary>
public class SimpleOpenTelemetryConfiguration : SimpleOpenTelemetryBuilderOptions
{
    /// <summary>
    /// Section name in configuration files 
    /// </summary>
    public const string SectionName = "OpenTelemetry";
}