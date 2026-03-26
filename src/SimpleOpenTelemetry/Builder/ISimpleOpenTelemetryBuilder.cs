using OpenTelemetry;

namespace SimpleOpenTelemetry.Builder;

/// <summary>
/// Interface for the SimpleOpenTelemetry configuration builder
/// </summary>
internal interface ISimpleOpenTelemetryBuilder
{
    /// <summary>
    /// Configure OpenTelemetry settings via IConfiguration and return
    /// OpenTelemetryBuilder for an other custom fluent operations
    /// </summary>
    /// <returns></returns>
    IOpenTelemetryBuilder Configure();
}
