namespace SimpleOpenTelemetry.OtelComponents.Exporter;

/// <summary>
/// Defines supported trace exporters available in SimpleOpenTelemetry.
/// </summary>
public enum TraceExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for trace export.</summary>
    Otlp,

    /// <summary>Exports traces to console output (for debugging).</summary>
    Console,

    /// <summary>Exports traces to Azure Monitor Application Insights.</summary>
    /* vendor libraries */
    AzureMonitor
}
