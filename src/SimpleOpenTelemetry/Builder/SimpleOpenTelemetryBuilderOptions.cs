using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Builder;

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

internal class SimpleOpenTelemetryMetricOptions
{

    /// <summary>
    ///
    /// </summary>
    public MetricInstrumentationEnum[]? Instrumentations { get; set; }

    public IConfigurationSection? InstrumentationConfig { get; set; }

    /// <summary>
    /// Defines which exporters to use for metrics.
    /// If otlp is specified, the standard OpenTelemetry ENV vars or config sections can be used
    /// Or override for specific alternate targets when wanting multiple otlp exports
    /// </summary>
    public List<SimpleOpenTelemetryExporterConfig>? Exporters { get; set; } = new();

    public MetricExtensionsEnum[]? Extensions { get; set; }

    public SimpleOpenTelemetryMeterProviderSettings? Settings { get; set; }
    
    /// <summary>
    /// Namespace names for additional metrics sources.
    /// </summary>
    public string[]? CustomMeters { get; set; }

}

internal class SimpleOpenTelemetryMeterProviderSettings
{
    public int? MetricLimit { get; set; }
    
}

internal class SimpleOpenTelemetryTraceOptions 
{
     /// <summary>
    ///
    /// </summary>
    public TraceInstrumentationEnum[]? Instrumentations { get; set; }

    public IConfigurationSection? InstrumentationConfig { get; set; }

    public List<SimpleOpenTelemetryExporterConfig>? Exporters { get; set; } = new();

    public TraceExtensionsEnum[]? Extensions { get; set; }

    public SimpleOpenTelemetryTraceProviderSettings? Settings { get; set; }

    /// <summary>
    /// Namespace names for additional trace sources. Wildcards accepted, eg Azure.*
    /// </summary>
    public string[]? Sources { get; set; }

    public string[]? Propagators { get; set; }

}

internal class SimpleOpenTelemetryTraceProviderSettings
{
    public bool? SetErrorStatusOnException { get; set; }
}

internal class SimpleOpenTelemetryLogOptions 
{
    /// <summary>
    /// Defines which exporters to use for logs.
    /// If otlp is specified, the standard OpenTelemetry ENV vars or config sections can be used
    /// Or override for specific alternate targets when wanting multiple otlp exports
    /// </summary>
    public List<SimpleOpenTelemetryExporterConfig>? Exporters { get; set; } = new();

    public LogExtensionsEnum[]? Extensions { get; set; }

    public SimpleOpenTelemetryLogProviderSettings? Settings { get;set; }
}

internal class SimpleOpenTelemetryLogProviderSettings
{
    public bool? IncludeFormattedMessage { get; set; }

    public bool? IncludeScopes { get; set; }
    
    public bool? ParseStateValues { get; set; }
}

/// <summary>
/// Configuration options for SimpleOpenTelemetry Builder
/// </summary>
internal class SimpleOpenTelemetryBuilderOptions
{
    public const string TraceSectionName = "Trace";
    public const string LogSectionName = "Log";
    public const string MetricSectionName = "Metric";

    public string? Distro { get; set; }

    public SimpleOpenTelemetryTraceOptions Trace { get; set; } = new();

    public SimpleOpenTelemetryMetricOptions Metric { get; set; } = new();
    
    public SimpleOpenTelemetryLogOptions Log { get; set; } = new();

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
