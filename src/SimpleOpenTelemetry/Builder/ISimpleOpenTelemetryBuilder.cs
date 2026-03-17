namespace SimpleOpenTelemetry.Builder;

/// <summary>
/// Interface for the SimpleOpenTelemetry configuration builder
/// </summary>
public interface ISimpleOpenTelemetryBuilder
{
    /// <summary>
    /// Configure OpenTelemetry settings via IConfiguration
    /// </summary>
    /// <returns></returns>
    ISimpleOpenTelemetryBuilder Configure();
}
