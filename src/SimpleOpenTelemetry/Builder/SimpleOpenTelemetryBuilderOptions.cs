using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Builder;

internal class SimpleOpenTelemetryExportersOptions
{
    public List<SimpleOpenTelemetryExporterConfig> Tracing { get; set; } = new();
    public List<SimpleOpenTelemetryExporterConfig> Logging { get; set; } = new();
    public List<SimpleOpenTelemetryExporterConfig> Metrics { get; set; } = new();
}

/// <summary>
/// Builtin supported OpenTelemetry SDK exporters
/// </summary>
public enum SimpleOpenTelemetryExporterType
{
    Otlp,
    Console,
    PrometheusHttpListener, // TODO Chad change these from here to end  to only lookup in exporter assemblies than both
    PrometheusAspNetCore,
    Azure, 

}

public enum SimpleOpenTelemetryExporterProtocol
{
    Grpc,
    Http
}

internal class SimpleOpenTelemetryExporterConfig
{
    public SimpleOpenTelemetryExporterType Type { get; set; }


    public IConfigurationSection? Options { get; set; }
}


/// <summary>
/// Configuration options for SimpleOpenTelemetry Builder
/// </summary>
internal class SimpleOpenTelemetryBuilderOptions
{
    /// <summary>
    /// Defines which exporters to use for traces, metrics, and logs.
    /// If otlp is specified, the standard OpenTelemetry ENV vars or config sections can be used
    /// Or override for specific alternate targets when wanting multiple otlp exports
    /// </summary>
    public SimpleOpenTelemetryExportersOptions? Exporters { get; set; } = new();

    /// <summary>
    ///
    /// </summary>
    public TracingInstrumentationEnum[]? TracingInstrumentations { get; set; }

    /// <summary>
    ///
    /// </summary>
    public MetricsInstrumentationEnum[]? MetricsInstrumentations { get; set; }

    public IConfigurationSection? TraceInstrumentationConfig { get; set; }

    /// <summary>
    /// Namespace names for additional metrics sources.
    /// </summary>
    public string[]? CustomMeters { get; set; }

    /// <summary>
    /// Namespace names for additional trace sources. Wildcards accepted, eg Azure.*
    /// </summary>
    public string[]? TraceSources { get; set; }

    /// <summary>
    /// Options for Vendor distro exporters
    /// </summary>
    public IConfigurationSection? ExporterOptions { get; set; }

    /// <summary>
    /// Register resource detectors set by resource type name eg: aws, azure etc
    /// </summary>
    public string[]? ResourceDetectors { get; set; }

    public IConfigurationSection? ResourceDetectorConfig { get; set; }

    /// <summary>
    /// Register a vendor sampler from the availble enum set 
    /// </summary>
    public string? Sampler { get; set; }
}
