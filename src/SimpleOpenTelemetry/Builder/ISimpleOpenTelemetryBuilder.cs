namespace SimpleOpenTelemetry.Builder;

using OpenTelemetry.Trace;

/// <summary>
/// Interface for the fluent OpenTelemetry configuration builder
/// </summary>
public interface ISimpleOpenTelemetryBuilder
{
    /// <summary>
    /// Gets the underlying TracerProviderBuilder for direct configuration
    /// </summary>
    TracerProviderBuilder TracerProviderBuilder { get; }

    /// <summary>
    /// Sets the service name
    /// </summary>
    ISimpleOpenTelemetryBuilder WithServiceName(string serviceName);

    /// <summary>
    /// Sets the service version
    /// </summary>
    ISimpleOpenTelemetryBuilder WithServiceVersion(string serviceVersion);

    /// <summary>
    /// Configures tracing options
    /// </summary>
    ISimpleOpenTelemetryBuilder ConfigureTracing(Action<TracerProviderBuilder> configure);

    /// <summary>
    /// Builds and returns the TracerProvider
    /// </summary>
    TracerProvider Build();
}
