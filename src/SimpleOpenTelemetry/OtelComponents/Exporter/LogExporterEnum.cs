namespace SimpleOpenTelemetry.OtelComponents.Exporter;

/// <summary>
/// Defines supported log exporters available in SimpleOpenTelemetry.
/// </summary>
public enum LogExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for log export.</summary>
    Otlp,

    /// <summary>Exports logs to console output (for debugging).</summary>
    Console,

    /// <summary>Exports logs to Azure Monitor Application Insights.</summary>
    /* vendor libraries */
    AzureMonitor
}
