using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;

namespace SimpleOpenTelemetry;

/// <summary>
/// 
/// </summary>
public enum SimpleOpenTelemetryExporterProtocol
{
    /// <summary>
    /// gRPC protocol for exporting telemetry.
    /// </summary>
    Grpc,
    /// <summary>
    /// HTTP protocol for exporting telemetry.
    /// </summary>
    Http
}


/// <summary>
/// Configuration options for SimpleOpenTelemetry
/// </summary>
internal class SimpleOpenTelemetryOptions
{
    public const string SectionName = "SimpleOpenTelemetry";

    /// <summary>
    /// Use a specific distribution - this bypasses all other settings
    /// </summary>
    public string? Distro { get; set; }

    /// <summary>
    /// OpenTelemetry tracing settings
    /// </summary>
    public SimpleOpenTelemetryTraceOptions Trace { get; set; } = new();

    /// <summary>
    /// OpenTelemetry metrics settings
    /// </summary>
    public SimpleOpenTelemetryMetricOptions Metric { get; set; } = new();

    /// <summary>
    /// OpenTelemetry Log settings
    /// </summary>
    public SimpleOpenTelemetryLogOptions Log { get; set; } = new();

    /// <summary>
    /// Options for 3rd party Vendor exporters
    /// </summary>
    public IConfigurationSection? ExporterOptions { get; set; }

    /// <summary>
    /// OpenTelemetry resource related settings (detectors etc)
    /// </summary>
    public ResourceOptions? Resource { get; set; }

}

internal class SimpleOpenTelemetryExporterConfig<TEnum>
{
    public TEnum? Type { get; set; }

    public IConfigurationSection? Options { get; set; }
}

internal class SimpleOpenTelemetryMetricOptions
{
    public MetricInstrumentationEnum[]? Instrumentations { get; set; }

    public IConfigurationSection? InstrumentationConfig { get; set; }

    /// <summary>
    /// Defines which exporters to use for metrics.
    /// If otlp is specified, the standard OpenTelemetry ENV vars or config sections can be used
    /// Or override for specific alternate targets when wanting multiple otlp exports
    /// </summary>
    public List<SimpleOpenTelemetryExporterConfig<MetricExporterEnum>>? Exporters { get; set; } = new();

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
    public TraceInstrumentationEnum[]? Instrumentations { get; set; }

    public IConfigurationSection? InstrumentationConfig { get; set; }

    public List<SimpleOpenTelemetryExporterConfig<TraceExporterEnum>>? Exporters { get; set; } = new();

    public TraceExtensionsEnum[]? Extensions { get; set; }

    public SimpleOpenTelemetryTraceProviderSettings? Settings { get; set; }

    /// <summary>
    /// Namespace names for additional trace sources. Wildcards accepted, eg Azure.*
    /// </summary>
    public string[]? Sources { get; set; }

    public string[]? Propagators { get; set; }

    
    /// <summary>
    /// Register a vendor sampler from the availble enum set 
    /// </summary>
    public string? Sampler { get; set; }
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
    public List<SimpleOpenTelemetryExporterConfig<LogExporterEnum>>? Exporters { get; set; } = new();

    public LogExtensionsEnum[]? Extensions { get; set; }

    public SimpleOpenTelemetryLogProviderSettings? Settings { get;set; }
}

internal class SimpleOpenTelemetryLogProviderSettings
{
    public bool? IncludeFormattedMessage { get; set; }

    public bool? IncludeScopes { get; set; }
    
    public bool? ParseStateValues { get; set; }
}

internal class ResourceOptions
{
    
    /// <summary>
    /// Register resource detectors set by resource type name eg: aws, azure etc
    /// </summary>
    public string[]? Detectors { get; set; }

    public IConfigurationSection? DetectorConfig { get; set; }

}

