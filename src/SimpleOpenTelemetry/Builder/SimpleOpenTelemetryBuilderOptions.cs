using OpenTelemetry;
using OpenTelemetry.Exporter;
using System;

namespace SimpleOpenTelemetry.Builder;

public class SimpleOpenTelemetryExportersOptions
{
    public List<SimpleOpenTelemetryExporterConfig> Tracing { get; set; } = new();
    public List<SimpleOpenTelemetryExporterConfig> Logging { get; set; } = new();
    public List<SimpleOpenTelemetryExporterConfig> Metrics { get; set; } = new();
}

public enum SimpleOpenTelemetryExporterType
{
    Otlp,
    Console,
    Memory
}

public enum SimpleOpenTelemetryExporterProtocol
{
    Grpc,
    Http
}

public class SimpleOpenTelemetryExporterConfig
{
    public SimpleOpenTelemetryExporterType Type { get; set; }
    public Uri? Endpoint { get; set; }
    public SimpleOpenTelemetryExporterProtocol? Protocol { get; set; }

    /// <inheritdoc/>
    public string? Headers { get; set; }

    /// <inheritdoc/>
    public int? TimeoutMilliseconds { get; set; }
}

public class InstrumentationOptions
{

    /// <summary>
    /// Enable AspNetCoreInstrumentation
    /// </summary>
    public bool? AspNetCoreInstrumentation { get; set; }

    /// <summary>
    /// Enable HttpClientInstrumentation
    /// </summary>
    public bool? HttpClientInstrumentation { get; set; }

    /// <summary>
    /// Enable SqlClientInstrumentation
    /// </summary>
    public bool? SqlClientInstrumentation { get; set; }

    /// <summary>
    /// Enable EFCoreInstrumentation
    /// </summary>
    public bool? EFCoreInstrumentation { get; set; }

    /// <summary>
    /// Send traces for Azure SDK operations (e.g., Azure.Storage.Blobs, Azure.Messaging.ServiceBus, etc.)
    /// </summary>
    public bool? AzureSDKTracing { get; set; }

    /// <summary>
    /// Enable AddRuntimeInstrumentation
    /// </summary>
    public bool? AddRuntimeInstrumentation { get; set; }
}

public enum AppTypeMonitoringPreset
{
    AspnetCore
    // TODO Chad add more
}

/// <summary>
/// Configuration options for SimpleOpenTelemetry Builder
/// </summary>
public class SimpleOpenTelemetryBuilderOptions
{
    /// <summary>
    /// Preset features / monitoring based on app type
    /// </summary>
    public AppTypeMonitoringPreset? AppTypeMonitoringPresets { get; set; } = null;

    /// <summary>
    /// Defines which exporters to use for traces, metrics, and logs.
    /// If otlp is specified, the standard OpenTelemetry ENV vars or config sections can be used
    /// Or override for specific alternate targets when wanting multiple otlp exports
    /// </summary>
    public SimpleOpenTelemetryExportersOptions Exporters { get; set; } = new();

    /// <summary>
    /// Instrumentation features to enable / disable over presets
    /// </summary>
    public InstrumentationOptions? Features { get; set; }

}