using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.Configuration;

/// <summary>
/// Configuration to use with IConfiguration/IOptions for applications using SimpleOpenTelemetry 
/// </summary>
internal class SimpleOpenTelemetryConfiguration : SimpleOpenTelemetryBuilderOptions
{
    /// <summary>
    /// Section name in configuration files 
    /// </summary>
    public const string SectionName = "SimpleOpenTelemetry";
}