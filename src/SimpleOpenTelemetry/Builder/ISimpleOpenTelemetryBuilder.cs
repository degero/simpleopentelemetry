namespace SimpleOpenTelemetry.Builder;

using OpenTelemetry;
using OpenTelemetry.Trace;

/// <summary>
/// Interface for the fluent OpenTelemetry configuration builder
/// </summary>
public interface ISimpleOpenTelemetryBuilder
{
    // /// <summary>
    // /// Sets the service name
    // /// </summary>
    // ISimpleOpenTelemetryBuilder WithServiceName(string serviceName);

    // /// <summary>
    // /// Sets the service version
    // /// </summary>
    // ISimpleOpenTelemetryBuilder WithServiceVersion(string serviceVersion);

    /// <summary>
    /// Enable tracing
    /// </summary>
    ISimpleOpenTelemetryBuilder WithTracing();
    /// <summary>
    /// Enable tracing with additional configuration options
    /// </summary>
    /// <returns></returns>
    ISimpleOpenTelemetryBuilder WithLogging();
    /// <summary>
    ///  Enable tracing with additional configuration options
    /// </summary>
    /// <returns></returns>
    ISimpleOpenTelemetryBuilder WithMetrics();

    /// <summary>
    ///     Enable tracing with additional configuration options
    /// </summary>
    /// <returns></returns>
    IOpenTelemetryBuilder AddOpenTelemetry();

    OpenTelemetryBuilder OtelBuilder { get; }

}
