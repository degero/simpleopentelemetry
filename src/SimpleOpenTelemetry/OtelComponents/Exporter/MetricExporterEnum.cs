namespace SimpleOpenTelemetry.OtelComponents.Exporter;

/// <summary>
/// Defines supported metrics exporters available in SimpleOpenTelemetry.
/// </summary>
public enum MetricExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for metrics export.</summary>
    Otlp,

    /// <summary>Exports metrics to console output (for debugging).</summary>
    Console,

    /// <summary>Prometheus metrics exporter with HTTP listener.</summary>
    PrometheusHttpListener,

    /// <summary>Prometheus metrics exporter integrated with ASP.NET Core.</summary>
    PrometheusAspNetCore,

    /* vendor libraries */
    /// <summary>Exports metrics to Azure Monitor Application Insights.</summary>
    AzureMonitor
}
