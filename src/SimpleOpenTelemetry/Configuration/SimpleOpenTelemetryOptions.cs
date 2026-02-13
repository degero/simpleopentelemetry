namespace SimpleOpenTelemetry.Configuration;

/// <summary>
/// Configuration options for SimpleOpenTelemetry
/// </summary>
public class SimpleOpenTelemetryOptions
{
    // TODO Chad make these optional as otel base libraries use OTEL_SERVICE_NAME and OTEL_SERVICE_VERSION environment variables by default

    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version
    /// </summary>
    public string? ServiceVersion { get; set; }
}
