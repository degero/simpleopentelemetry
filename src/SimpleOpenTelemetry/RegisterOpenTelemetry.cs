namespace SimpleOpenTelemetry;

using SimpleOpenTelemetry.Builder;

/// <summary>
/// Factory class for creating and configuring OpenTelemetry builders
/// </summary>
public static class RegisterOpenTelemetry
{
    /// <summary>
    /// Creates a new SimpleOpenTelemetry builder
    /// </summary>
    /// <returns>A new builder instance</returns>
    public static ISimpleOpenTelemetryBuilder CreateBuilder() =>
        new SimpleOpenTelemetryBuilder();
}
